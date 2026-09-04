using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using QuestingBots.Utils.Benchmarking;
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
        private const float MAX_CYCLE_TIME_MS = 1;

        public bool IsCreatingAnAssignment { get; private set; } = false;
        public bool NewAssignmentReady { get; private set; } = false;

        private BotOwner _botOwner;
        private BotObjectiveManager _objectiveManager = null!;
        private Stopwatch _timeoutMonitor = new Stopwatch();
        private Stopwatch _cycleTimer = new Stopwatch();
        private System.Random _random = new System.Random();
        private BotJobAssignment? _assignmentCreationResult = null;
        private BotQuest? _nextRandomQuest = null;

        public BotJobAssignment? AssignmentCreationResult => NewAssignmentReady ? _assignmentCreationResult : null;

        private bool jobHasBeenRunningTooLong => _timeoutMonitor.ElapsedMilliseconds > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.QuestSelectionTimeout;
        private double elapsedCycleTime => (double)_cycleTimer.ElapsedTicks / (double)Stopwatch.Frequency;
        private bool maxCycleTimeExceeded => elapsedCycleTime > MAX_CYCLE_TIME_MS;

        public BotJobAssignmentCreationJob(BotOwner botOwner)
        {
            _botOwner = botOwner;

            BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot retrieve BotObjectiveManager for " + _botOwner.GetText());
                return;
            }

            _objectiveManager = objectiveManager;
        }

        public IEnumerator CreateNewBotJobAssignment()
        {
            _assignmentCreationResult = null;
            IsCreatingAnAssignment = true;
            NewAssignmentReady = false;

            try
            {
                 yield return TryGetNextAssignment();

                if (_assignmentCreationResult != null)
                {
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

        private IEnumerator TryGetNextAssignment()
        {
            _timeoutMonitor.Restart();
            _cycleTimer.Restart();

            BotJobAssignment? mostRecentAssignment = _botOwner.GetMostRecentJobAssignment();
            BotQuest? quest = mostRecentAssignment?.QuestAssignment;
            BotQuestObjective? objective = GetNextObjectiveForQuest(quest);

            while ((quest == null) || (objective == null))
            {
                yield return ChooseNextRandomQuest();

                quest = _nextRandomQuest;
                objective = GetNextObjectiveForQuest(quest);
                
                // If a quest hasn't been found within a certain amount of time, something is wrong
                if ((objective == null) && jobHasBeenRunningTooLong)
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Waited " + _timeoutMonitor.ElapsedMilliseconds + "ms to select a quest for " + _botOwner.GetText());

                    // First try allowing the bot to repeat quests it already completed
                    if (_botOwner.TryArchiveRepeatableAssignments() > 0)
                    {
                        Singleton<LoggingUtil>.Instance.LogWarning(_botOwner.GetText() + " cannot select any quests. Trying to select a repeatable quest early instead...");
                        continue;
                    }

                    StopQuestingAndExtract();
                    yield break;
                }
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Waited " + _timeoutMonitor.ElapsedMilliseconds + "ms to select a quest for " + _botOwner.GetText());
            _assignmentCreationResult = new BotJobAssignment(_botOwner, quest, objective);
        }

        [Benchmark]
        private BotQuestObjective? GetNextObjectiveForQuest(BotQuest? quest)
        {
            if (quest == null)
            {
                return null;
            }

            // Clear the bot's assignment if it's been doing the same quest for too long
            if ((quest.HasBotBeingDoingQuestTooLong(_botOwner, out double? timeDoingQuest) == true) && (timeDoingQuest != null))
            {
                Singleton<LoggingUtil>.Instance.LogInfo(_botOwner.GetText() + " has been performing quest " + quest.ToString() + " for " + timeDoingQuest.Value + "s and will get a new one.");
                return null;
            }

            return quest
                .RemainingObjectivesForBot(_botOwner)
                .Where(o => o.CanAssignBot(_botOwner))
                .Where(o => o.CanBotRepeatQuestObjective(_botOwner))
                .NearestToBot(_botOwner);
        }

        private void StopQuestingAndExtract()
        {
            // If there are still no quests available for the bot to select, give up trying to select one
            Singleton<LoggingUtil>.Instance.LogError(_botOwner.GetText() + " could not select any of the following quests: " + string.Join(", ", _botOwner.GetAllPossibleQuests()));
            _objectiveManager.StopQuesting();

            // Try making the bot extract because it has nothing to do
            if (_objectiveManager.BotMonitor.GetMonitor<BotExtractMonitor>().TryInstructBotToExtract())
            {
                Singleton<LoggingUtil>.Instance.LogWarning(_botOwner.GetText() + " cannot select any quests. Extracting instead...");
                return;
            }

            Singleton<LoggingUtil>.Instance.LogError(_botOwner.GetText() + " cannot select any quests. Questing disabled.");
        }

        private IEnumerator ChooseNextRandomQuest()
        {
            _nextRandomQuest = null;

            IEnumerable<BotQuest> assignableQuests = _botOwner.GetAllPossibleQuests();
            if (!assignableQuests.Any())
            {
                yield break;
            }

            Dictionary<BotQuest, Configuration.MinMaxConfig> questDistanceRanges = GetQuestDistanceRanges(assignableQuests);
            Dictionary<BotQuest, Configuration.MinMaxConfig> questExfilAngleRanges = GetQuestExfilAngleRanges(assignableQuests);

            double maxDistance = questDistanceRanges.Max(o => o.Value.Max);
            int distanceRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceRandomness;
            int maxRandomDistance = (int)Math.Ceiling(maxDistance * distanceRandomness / 100.0);
            float maxExfilAngle = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionMaxAngle;

            int desirabilityRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityRandomness;

            float distanceWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceWeighting;
            float desirabilityWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityWeighting;
            float exfilDirectionWeighting = GetExfilWeighting();

            Dictionary<BotQuest, double> questWeights = new Dictionary<BotQuest, double>();
            foreach (BotQuest quest in assignableQuests)
            {
                Configuration.MinMaxConfig distanceRange = questDistanceRanges[quest];
                Configuration.MinMaxConfig exfilAngleRange = questExfilAngleRanges[quest];

                double distanceFraction = 1 - ((distanceRange.Min + _random.Next(-1 * maxRandomDistance, maxRandomDistance)) / maxDistance);
                double desirabilityFraction = (quest.Desirability * DesirabilityMultiplier(quest) + _random.Next(-1 * desirabilityRandomness, desirabilityRandomness)) / 100;
                double exfilAngleFactor = Math.Max(0, exfilAngleRange.Min - maxExfilAngle) / (180 - maxExfilAngle);

                double weight = (distanceFraction * distanceWeighting) + (desirabilityFraction * desirabilityWeighting) + (exfilAngleFactor * exfilDirectionWeighting);
                questWeights.Add(quest, weight);

                yield return HasReachMaxCalculationTimeForFrame();
            }

            _nextRandomQuest = questWeights
                .OrderBy(o => o.Value)
                .Last().Key;

            //Singleton<LoggingUtil>.Instance.LogInfo("Distance: " + questDistanceFractions[selectedQuest] + ", Desirability: " + questDesirabilityFractions[selectedQuest] + ", Exfil Angle Factor: " + questExfilAngleFactor[selectedQuest]);
        }

        private IEnumerator HasReachMaxCalculationTimeForFrame()
        {
            if (maxCycleTimeExceeded)
            {
                yield return null;
                _cycleTimer.Restart();
            }
        }

        [Benchmark]
        private Dictionary<BotQuest, Configuration.MinMaxConfig> GetQuestDistanceRanges(IEnumerable<BotQuest> quests)
        {
            Dictionary<BotQuest, Configuration.MinMaxConfig> questDistanceRanges = new Dictionary<BotQuest, Configuration.MinMaxConfig>();

            foreach (BotQuest quest in quests)
            {
                IEnumerable<Vector3> validObjectivePositions = GetValidObjectivePositions(quest);
                IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(_botOwner.Position, p));

                questDistanceRanges.Add(quest, new Configuration.MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));
            }

            return questDistanceRanges;
        }

        [Benchmark]
        private Dictionary<BotQuest, Configuration.MinMaxConfig> GetQuestExfilAngleRanges(IEnumerable<BotQuest> quests)
        {
            Dictionary<BotQuest, Configuration.MinMaxConfig> questExfilAngleRanges = new Dictionary<BotQuest, Configuration.MinMaxConfig>();

            Vector3? vectorToExfil = _objectiveManager.QuestSelector.VectorToExfiltrationPointForQuesting();

            foreach (BotQuest quest in quests)
            {
                if (vectorToExfil == null)
                {
                    questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(0, 0));
                    continue;
                }

                IEnumerable<Vector3> validObjectivePositions = GetValidObjectivePositions(quest);
                IEnumerable<Vector3> vectorsToObjectivePositions = validObjectivePositions.Select(p => p - _botOwner.Position);
                IEnumerable<float> anglesToObjectives = vectorsToObjectivePositions.Select(p => Vector3.Angle(p - _botOwner.Position, vectorToExfil.Value));

                questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(anglesToObjectives.Min(), anglesToObjectives.Max()));
            }

            return questExfilAngleRanges;
        }

        private IEnumerable<Vector3> GetValidObjectivePositions(BotQuest quest)
        {
            foreach (BotQuestObjective objective in quest.ValidObjectives)
            {
                Vector3? firstPosition = objective.GetFirstStepPosition();
                if (firstPosition.HasValue)
                {
                    yield return firstPosition.Value;
                }
            }
        }

        private float DesirabilityMultiplier(BotQuest quest)
        {
            if (quest.IsActiveForPlayer)
            {
                return Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityActiveQuestMultiplier;
            }

            return 1;
        }

        private float GetExfilWeighting()
        {
            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey(locationId))
            {
                return Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting[locationId];
            }
            else if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey("default"))
            {
                return Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionWeighting["default"];
            }

            return 0;
        }
    }
}
