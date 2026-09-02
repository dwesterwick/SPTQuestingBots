using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using QuestingBots.Utils.Benchmarking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Components
{
    public class BotQuestSelector : BehaviorExtensions.MonoBehaviourDelayedUpdate
    {
        private BotOwner _botOwner = null!;
        private ExfiltrationPoint exfiltrationPoint = null!;

        public static BotQuestSelector GetBotQuestSelector(BotOwner botOwner)
        {
            BotQuestSelector botQuestSelector = botOwner.gameObject.GetOrAddComponent<BotQuestSelector>();
            botQuestSelector.Init(botOwner);

            return botQuestSelector;
        }

        public void Init(BotOwner botOwner)
        {
            _botOwner = botOwner;
            SetExfiliationPointForQuesting();
        }

        [Benchmark]
        public BotJobAssignment? GetInitialObjective()
        {
            BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                return null;
            }

            // Only set an objective for the bot if its type is allowed to spawn and all quests have been loaded and generated
            if (!objectiveManager.IsQuestingAllowed || !Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                return null;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Setting objective for " + _botOwner.GetText() + " (Brain type: " + _botOwner.Brain.BaseBrain.ShortName() + ")...");
            try
            {
                BotJobAssignment? botJobAssignment = GetCurrentJobAssignment();
                return botJobAssignment;
            }
            catch (TimeoutException)
            {
                Singleton<LoggingUtil>.Instance.LogError("Timed out when trying to select an initial objective for " + _botOwner.GetText());
            }

            return null;
        }

        [Benchmark]
        public void SetExfiliationPointForQuesting()
        {
            Dictionary<ExfiltrationPoint, float> exfiltrationPointDistances = Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints
                .ToDictionary(p => p, p => Vector3.Distance(p.transform.position, _botOwner.Position));

            if (exfiltrationPointDistances.Count > 0)
            {
                KeyValuePair<ExfiltrationPoint, float> furthestPoint = exfiltrationPointDistances
                    .OrderBy(p => p.Value)
                    .Last();

                exfiltrationPoint = furthestPoint.Key;

                //Singleton<LoggingUtil>.Instance.LogInfo(botOwner.GetText() + " has selected " + furthestPoint.Key.Settings.Name + " as its furthest exfil point (" + furthestPoint.Value + "m)");
            }
        }

        public float? DistanceToExfiltrationPointForQuesting()
        {
            if (exfiltrationPoint == null)
            {
                return null;
            }

            return Vector3.Distance(_botOwner.Position, exfiltrationPoint.transform.position);
        }

        public Vector3? VectorToExfiltrationPointForQuesting()
        {
            if (exfiltrationPoint == null)
            {
                return null;
            }

            return exfiltrationPoint.transform.position - _botOwner.Position;
        }

        public BotJobAssignment? GetCurrentJobAssignment(bool allowUpdate = true)
        {
            bool hasNewJobAssignment = allowUpdate && DoesBotHaveNewJobAssignment();
            BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();

            if (hasNewJobAssignment && (mostRecentAssignment != null))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Bot " + _botOwner.GetText() + " is now doing " + mostRecentAssignment.ToString());

                ReadOnlyCollection<BotJobAssignment> allJobAssignments = _botOwner.GetAllBotJobAssignments();

                if (allJobAssignments.Count > 1)
                {
                    BotJobAssignment lastAssignment = allJobAssignments.TakeLast(2).First();
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + _botOwner.GetText() + " was previously doing " + lastAssignment.ToString());

                    //double? timeSinceBotStartedQuest = lastAssignment.QuestAssignment.ElapsedTimeSinceBotStarted(bot);
                    //double? timeSinceBotLastFinishedQuest = lastAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(bot);
                    //string startedTimeText = timeSinceBotStartedQuest.HasValue ? timeSinceBotStartedQuest.Value.ToString() : "N/A";
                    //string lastFinishedTimeText = timeSinceBotLastFinishedQuest.HasValue ? timeSinceBotLastFinishedQuest.Value.ToString() : "N/A";
                    //Singleton<LoggingUtil>.Instance.LogInfo("Time since first objective ended: " + startedTimeText + ", Time since last objective ended: " + lastFinishedTimeText);
                }
            }

            if (allowUpdate && (mostRecentAssignment == null))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not get a job assignment for bot " + _botOwner.GetText());
            }

            return mostRecentAssignment;
        }

        public bool DoesBotHaveNewJobAssignment()
        {
            BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();
            if (mostRecentAssignment != null)
            {
                // Check if the bot is currently doing an assignment
                if (mostRecentAssignment.IsActive)
                {
                    return false;
                }

                // Check if more steps are available for the bot's current assignment
                if (mostRecentAssignment.TrySetNextObjectiveStep(false))
                {
                    return true;
                }

                //Singleton<LoggingUtil>.Instance.LogInfo("There are no more steps available for " + bot.GetText() + " in " + (currentAssignment.QuestObjectiveAssignment?.ToString() ?? "???"));
            }

            BotJobAssignment? newJobAssignment = GetNewBotJobAssignment();
            if (newJobAssignment != null)
            {
                return true;
            }

            return false;
        }

        [Benchmark]
        public BotJobAssignment? GetNewBotJobAssignment()
        {
            if (_botOwner == null)
            {
                throw new ArgumentNullException("Cannot get an assignment for a null bot");
            }

            BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                return null;
            }

            // Do not select another quest objective if the bot wants to extract
            if (objectiveManager.DoesBotWantToExtract())
            {
                return null;
            }

            float maxDistanceBetweenExfils = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetMaxDistanceBetweenExfils();
            float minDistanceToSwitchExfil = maxDistanceBetweenExfils * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilReachedMinFraction;

            // If the bot is close to its selected exfil (only used for quest selection), select a new one
            float? distanceToExfilPoint = objectiveManager.QuestSelector.DistanceToExfiltrationPointForQuesting();
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
                        return null;
                    }

                    Singleton<LoggingUtil>.Instance.LogError(_botOwner.GetText() + " cannot select any quests. Questing disabled.");
                    return null;
                }

            } while (objective == null);

            if (quest == null)
            {
                return null;
            }

            // Once a valid assignment is selected, assign it to the bot
            BotJobAssignment assignment = new BotJobAssignment(_botOwner, quest, objective);
            assignment.Register();

            return assignment;
        }

        public BotQuest GetRandomQuest(IEnumerable<BotQuest> invalidQuests)
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
    }
}
