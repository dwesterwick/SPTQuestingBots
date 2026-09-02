using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public class BotJobAssignmentCreationJob
    {
        public bool IsCreatingAnAssignment { get; private set; } = false;
        public bool NewAssignmentReady { get; private set; } = false;

        private BotOwner _botOwner;
        private ExfiltrationPoint _exfiltrationPoint;

        private BotJobAssignment? _assignmentCreationResult = null;
        public BotJobAssignment? AssignmentCreationResult => NewAssignmentReady ? _assignmentCreationResult : null;

        public BotJobAssignmentCreationJob(BotOwner botOwner, ExfiltrationPoint exfiltrationPoint)
        {
            _botOwner = botOwner;
            _exfiltrationPoint = exfiltrationPoint;
        }

        public IEnumerator CreateNewBotJobAssignment()
        {
            _assignmentCreationResult = null;
            IsCreatingAnAssignment = true;
            NewAssignmentReady = false;

            try
            {
                BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
                if (objectiveManager == null)
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                    yield break;
                }

                float maxDistanceBetweenExfils = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetMaxDistanceBetweenExfils();
                float minDistanceToSwitchExfil = maxDistanceBetweenExfils * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilReachedMinFraction;

                // If the bot is close to its selected exfil (only used for quest selection), select a new one
                float? distanceToExfilPoint = DistanceToExfiltrationPointForQuesting();
                if (distanceToExfilPoint.HasValue && (distanceToExfilPoint.Value < minDistanceToSwitchExfil))
                {
                    objectiveManager.QuestSelector.SetExfiliationPointForQuesting();
                }

                // Get the bot's most recent assingment if applicable
                BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();
                BotQuest? quest = mostRecentAssignment?.QuestAssignment;
                BotQuestObjective? objective = mostRecentAssignment?.QuestObjectiveAssignment;

                // Clear the bot's assignment if it's been doing the same quest for too long
                if ((quest?.HasBotBeingDoingQuestTooLong(_botOwner, out double? timeDoingQuest) == true) && (timeDoingQuest != null))
                {
                    Singleton<LoggingUtil>.Instance.LogInfo(_botOwner.GetText() + " has been performing quest " + quest.ToString() + " for " + timeDoingQuest.Value + "s and will get a new one.");
                    quest = null;
                    objective = null;
                }

                // Try to find a quest that has at least one objective that can be assigned to the bot
                List<BotQuest> invalidQuests = new List<BotQuest>();
                Stopwatch timeoutMonitor = Stopwatch.StartNew();
                do
                {
                    yield return null;

                    // Find the nearest objective for the bot's currently assigned quest (if any)
                    objective = quest?
                        .RemainingObjectivesForBot(_botOwner)?
                        .Where(o => o.CanAssignBot(_botOwner))?
                        .Where(o => o.CanBotRepeatQuestObjective(_botOwner))?
                        .NearestToBot(_botOwner);

                    // Exit the loop if an objective was found for the bot
                    if (objective != null)
                    {
                        break;
                    }
                    if (quest != null)
                    {
                        //Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " cannot select quest " + quest.ToString() + " because it has no valid objectives");
                        invalidQuests.Add(quest);
                    }

                    // If no objectives were found, select another quest
                    quest = GetRandomQuest(invalidQuests);

                    // If a quest hasn't been found within a certain amount of time, something is wrong
                    if (timeoutMonitor.ElapsedMilliseconds > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.QuestSelectionTimeout)
                    {
                        // First try allowing the bot to repeat quests it already completed
                        if (_botOwner.TryArchiveRepeatableAssignments() > 0)
                        {
                            Singleton<LoggingUtil>.Instance.LogWarning(_botOwner.GetText() + " cannot select any quests. Trying to select a repeatable quest early instead...");
                            continue;
                        }

                        // If there are still no quests available for the bot to select, give up trying to select one
                        Singleton<LoggingUtil>.Instance.LogError(_botOwner.GetText() + " could not select any of the following quests: " + string.Join(", ", _botOwner.GetAllPossibleQuests()));
                        objectiveManager.StopQuesting();

                        // Try making the bot extract because it has nothing to do
                        if (objectiveManager.BotMonitor.GetMonitor<BotExtractMonitor>().TryInstructBotToExtract())
                        {
                            Singleton<LoggingUtil>.Instance.LogWarning(_botOwner.GetText() + " cannot select any quests. Extracting instead...");
                            yield break;
                        }

                        Singleton<LoggingUtil>.Instance.LogError(_botOwner.GetText() + " cannot select any quests. Questing disabled.");
                        yield break;
                    }

                } while (objective == null);

                if (quest != null)
                {
                    _assignmentCreationResult = new BotJobAssignment(_botOwner, quest, objective);
                    _assignmentCreationResult.Register();
                    NewAssignmentReady = true;
                }
                else
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Could not get a job assignment for bot " + _botOwner.GetText());
                }
            }
            finally
            {
                IsCreatingAnAssignment = false;
            }
        }

        private BotQuest GetRandomQuest(IEnumerable<BotQuest> invalidQuests)
        {
            if (_botOwner == null)
            {
                throw new ArgumentNullException("Cannot get a quest for a null bot");
            }

            Stopwatch questSelectionTimer = Stopwatch.StartNew();

            BotQuest[] assignableQuests = _botOwner.GetAllPossibleQuests()
                .Where(q => !invalidQuests.Contains(q))
                .ToArray();

            if (!assignableQuests.Any())
            {
                return null!;
            }

            Vector3? vectorToExfil = VectorToExfiltrationPointForQuesting();

            Dictionary<BotQuest, Configuration.MinMaxConfig> questDistanceRanges = new Dictionary<BotQuest, Configuration.MinMaxConfig>();
            Dictionary<BotQuest, Configuration.MinMaxConfig> questExfilAngleRanges = new Dictionary<BotQuest, Configuration.MinMaxConfig>();

            // Calculate the distances from the bot to all valid quest objectives and the angles between the vector to the bot's selected
            // exfil (for questing) and the vector to each valid quest objective
            foreach (BotQuest quest in assignableQuests)
            {
                IEnumerable<Vector3?> objectivePositions = quest.ValidObjectives.Select(o => o.GetFirstStepPosition());
                IEnumerable<Vector3> validObjectivePositions = objectivePositions.Where(p => p.HasValue).Select(p => p!.Value);
                IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(_botOwner.Position, p));

                questDistanceRanges.Add(quest, new Configuration.MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));

                if (vectorToExfil.HasValue)
                {
                    IEnumerable<Vector3> vectorsToObjectivePositions = validObjectivePositions.Select(p => p - _botOwner.Position);
                    IEnumerable<float> anglesToObjectives = vectorsToObjectivePositions.Select(p => Vector3.Angle(p - _botOwner.Position, vectorToExfil.Value));

                    questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(anglesToObjectives.Min(), anglesToObjectives.Max()));
                }
                else
                {
                    questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(0, 0));
                }
            }

            // Calculate the maximum amount of "randomness" to apply to each quest
            //double distanceRange = questDistanceRanges.Max(q => q.Value.Max) - questDistanceRanges.Min(q => q.Value.Min);
            double maxDistance = questDistanceRanges.Max(o => o.Value.Max);
            int maxRandomDistance = (int)Math.Ceiling(maxDistance * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceRandomness / 100.0);
            float maxExfilAngle = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionMaxAngle;

            int distanceRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceRandomness;
            int desirabilityRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityRandomness;

            float distanceWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceWeighting;
            float desirabilityWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityWeighting;
            float exfilDirectionWeighting = 0;

            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey(locationId))
            {
                exfilDirectionWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting[locationId];
            }
            else if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey("default"))
            {
                exfilDirectionWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting["default"];
            }

            System.Random random = new System.Random();
            Dictionary<BotQuest, double> questDistanceFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o => 1 - (o.Value.Min + random.Next(-1 * maxRandomDistance, maxRandomDistance)) / maxDistance);
            Dictionary<BotQuest, float> questDesirabilityFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o =>
                (
                    o.Key.Desirability * (o.Key.IsActiveForPlayer ? Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityActiveQuestMultiplier : 1)
                    + random.Next(-1 * desirabilityRandomness, desirabilityRandomness)) / 100
                );
            Dictionary<BotQuest, double> questExfilAngleFactor = questExfilAngleRanges
                .ToDictionary(o => o.Key, o => Math.Max(0, o.Value.Min - maxExfilAngle) / (180 - maxExfilAngle));

            IEnumerable<BotQuest> sortedQuests = questDistanceRanges
                .OrderBy
                (o =>
                    (questDistanceFractions[o.Key] * distanceWeighting)
                    + (questDesirabilityFractions[o.Key] * desirabilityWeighting)
                    - (questExfilAngleFactor[o.Key] * exfilDirectionWeighting)
                )
                .Select(o => o.Key);

            BotQuest selectedQuest = sortedQuests.Last();

            //Singleton<LoggingUtil>.Instance.LogInfo("Distance: " + questDistanceFractions[selectedQuest] + ", Desirability: " + questDesirabilityFractions[selectedQuest] + ", Exfil Angle Factor: " + questExfilAngleFactor[selectedQuest]);
            //Singleton<LoggingUtil>.Instance.LogInfo("Time for quest selection: " + questSelectionTimer.ElapsedMilliseconds + "ms");

            return selectedQuest;
        }

        private float? DistanceToExfiltrationPointForQuesting()
        {
            if (_exfiltrationPoint == null)
            {
                return null;
            }

            return Vector3.Distance(_botOwner.Position, _exfiltrationPoint.transform.position);
        }

        private Vector3? VectorToExfiltrationPointForQuesting()
        {
            if (_exfiltrationPoint == null)
            {
                return null;
            }

            return _exfiltrationPoint.transform.position - _botOwner.Position;
        }
    }
}
