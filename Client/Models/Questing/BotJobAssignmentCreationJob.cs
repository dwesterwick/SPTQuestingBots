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
    public class BotJobAssignmentCreationJob : IBotJobAssignmentCreationJob
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
            if (quest.HasBotBeingDoingQuestTooLong(_botOwner, out double? timeDoingQuest) && (timeDoingQuest != null))
            {
                Singleton<LoggingUtil>.Instance.LogInfo(_botOwner.GetText() + " has been performing quest " + quest.ToString() + " for " + timeDoingQuest.Value + "s and will get a new one.");
                return null;
            }

            IEnumerable<BotQuestObjective> remainingObjectives = quest.RemainingObjectivesForBot(_botOwner);
            return GetNearestAssignableObjective(remainingObjectives);
        }

        private BotQuestObjective? GetNearestAssignableObjective(IEnumerable<BotQuestObjective> assignableObjectives)
        {
            BotQuestObjective? nearestObjective = null;
            float nearestObjectiveDistance = float.MaxValue;
            foreach (BotQuestObjective objective in assignableObjectives)
            {
                if (!objective.CanAssignBot(_botOwner))
                {
                    continue;
                }

                if (!objective.CanBotSelectQuestObjective(_botOwner))
                {
                    continue;
                }

                Vector3? firstStepPosition = objective.GetFirstStepPosition();
                if (firstStepPosition == null)
                {
                    continue;
                }

                float objectiveDistance = Vector3.Distance(_botOwner.Position, firstStepPosition.Value);
                if (objectiveDistance < nearestObjectiveDistance)
                {
                    nearestObjective = objective;
                    nearestObjectiveDistance = objectiveDistance;
                }
            }

            return nearestObjective;
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

            BotQuest[] assignableQuests = _botOwner.GetAllPossibleQuests().ToArray();
            if (assignableQuests.Length == 0)
            {
                yield break;
            }

            Dictionary<BotQuest, Configuration.MinMaxConfig> questDistanceRanges = GetQuestDistanceRanges(assignableQuests);
            yield return HasReachMaxCalculationTimeForFrame();

            Dictionary<BotQuest, Configuration.MinMaxConfig> questExfilAngleRanges = GetQuestExfilAngleRanges(assignableQuests);
            yield return HasReachMaxCalculationTimeForFrame();

            double maxDistance = questDistanceRanges.Max(o => o.Value.Max);
            int distanceRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceRandomness;
            int maxRandomDistance = (int)Math.Ceiling(maxDistance * distanceRandomness / 100.0);
            float maxExfilAngle = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.ExfilDirectionMaxAngle;

            int desirabilityRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityRandomness;

            float distanceWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DistanceWeighting;
            float desirabilityWeighting = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityWeighting;
            float exfilDirectionWeighting = GetExfilWeighting();

            double maxWeight = double.MinValue;
            foreach (BotQuest quest in assignableQuests)
            {
                Configuration.MinMaxConfig distanceRange = questDistanceRanges[quest];
                Configuration.MinMaxConfig exfilAngleRange = questExfilAngleRanges[quest];

                double distanceFraction = 1 - ((distanceRange.Min + _random.Next(-1 * maxRandomDistance, maxRandomDistance)) / maxDistance);
                double desirabilityFraction = (quest.Desirability * DesirabilityMultiplier(quest) + _random.Next(-1 * desirabilityRandomness, desirabilityRandomness)) / 100;
                double exfilAngleFactor = Math.Max(0, exfilAngleRange.Min - maxExfilAngle) / (180 - maxExfilAngle);

                double weight = (distanceFraction * distanceWeighting) + (desirabilityFraction * desirabilityWeighting) + (exfilAngleFactor * exfilDirectionWeighting);
                if (weight > maxWeight)
                {
                    _nextRandomQuest = quest;
                    maxWeight = weight;
                }

                yield return HasReachMaxCalculationTimeForFrame();
            }
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
                if (!validObjectivePositions.Any())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("No valid positions found for quest " + quest.ToString());

                    questDistanceRanges.Add(quest, new Configuration.MinMaxConfig(float.MaxValue, float.MaxValue));
                    continue;
                }

                float minDistance = float.MaxValue;
                float maxDistance = 0;
                foreach (Vector3 objectivePosition in validObjectivePositions)
                {
                    float distance = Vector3.Distance(_botOwner.Position, objectivePosition);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }

                questDistanceRanges.Add(quest, new Configuration.MinMaxConfig(minDistance, maxDistance));
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
                    questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(float.MaxValue, float.MaxValue));
                    continue;
                }

                IEnumerable<Vector3> validObjectivePositions = GetValidObjectivePositions(quest);
                if (!validObjectivePositions.Any())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("No valid positions found for quest " + quest.ToString());

                    questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(float.MaxValue, float.MaxValue));
                    continue;
                }

                float minAngle = float.MaxValue;
                float maxAngle = float.MinValue;
                foreach (Vector3 objectivePosition in validObjectivePositions)
                {
                    float angle = Vector3.Angle(objectivePosition - _botOwner.Position, vectorToExfil.Value);
                    if (angle < minAngle)
                    {
                        minAngle = angle;
                    }
                    if (angle > maxAngle)
                    {
                        maxAngle = angle;
                    }
                }

                questExfilAngleRanges.Add(quest, new Configuration.MinMaxConfig(minAngle, maxAngle));
            }

            return questExfilAngleRanges;
        }

        private IEnumerable<Vector3> GetValidObjectivePositions(BotQuest quest)
        {
            foreach (BotQuestObjective objective in quest.GetValidObjectives())
            {
                Vector3? firstPosition = objective.GetFirstStepPosition();
                if (firstPosition == null)
                {
                    continue;
                }

                yield return firstPosition.Value;
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
