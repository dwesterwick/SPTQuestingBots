using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Aki.Common.Http;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Controllers
{
    public static class BotJobAssignmentFactory
    {
        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static List<Quest> allQuests = new List<Quest>();
        private static Dictionary<string, List<BotJobAssignment>> botJobAssignments = new Dictionary<string, List<BotJobAssignment>>();

        public static int QuestCount => allQuests.Count;

        public static Quest[] FindQuestsWithZone(string zoneId) => allQuests.Where(q => q.GetObjectiveForZoneID(zoneId) != null).ToArray();
        public static bool CanMoreBotsDoQuest(this Quest quest) => quest.NumberOfActiveBots() < quest.MaxBots;
        
        public static void Clear()
        {
            // Only remove quests that are not based on an EFT quest template
            allQuests.RemoveAll(q => q.Template == null);

            // Remove all objectives for remaining quests. New objectives will be generated after loading the map.
            foreach (Quest quest in allQuests)
            {
                quest.Clear();
            }

            botJobAssignments.Clear();
        }

        public static IEnumerator ProcessAllQuests(Action<Quest> action)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action);
        }

        public static IEnumerator ProcessAllQuests<T1>(Action<Quest, T1> action, T1 param1)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1);
        }

        public static IEnumerator ProcessAllQuests<T1, T2>(Action<Quest, T1, T2> action, T1 param1, T2 param2)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1, param2);
        }

        public static void AddQuest(Quest quest)
        {
            foreach(QuestObjective objective in quest.AllObjectives)
            {
                objective.UpdateQuestObjectiveStepNumbers();
            }

            allQuests.Add(quest);
        }

        public static Quest FindQuest(string questID)
        {
            IEnumerable<Quest> matchingQuests = allQuests.Where(q => q.TemplateId == questID);
            if (matchingQuests.Count() == 0)
            {
                return null;
            }

            return matchingQuests.First();
        }

        public static void RemoveBlacklistedQuestObjectives(string locationId)
        {
            foreach (Quest quest in allQuests.ToArray())
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();
                    if (!firstPosition.HasValue)
                    {
                        continue;
                    }

                    // Remove quests on Lightkeeper island. Otherwise, PMC's will engage you there when they normally wouldn't on live. 
                    if ((locationId == "Lighthouse") && (firstPosition.Value.x > 120) && (firstPosition.Value.z > 325))
                    {
                        if (quest.TryRemoveObjective(objective))
                        {
                            LoggingController.LogInfo("Removing quest objective on Lightkeeper island: " + objective.ToString() + " for quest " + quest.ToString());
                        }
                        else
                        {
                            LoggingController.LogError("Could not remove quest objective on Lightkeeper island: " + objective.ToString() + " for quest " + quest.ToString());
                        }

                        // If there are no remaining objectives, remove the quest too
                        if (quest.NumberOfObjectives == 0)
                        {
                            LoggingController.LogInfo("Removing quest on Lightkeeper island: " + quest.ToString() + "...");
                            allQuests.Remove(quest);
                        }
                    }
                }
            }
        }

        public static void FailAllJobAssignmentsForBot(string botID)
        {
            if (!botJobAssignments.ContainsKey(botID))
            {
                return;
            }

            foreach (BotJobAssignment assignment in botJobAssignments[botID].Where(a => a.IsActive))
            {
                assignment.Fail();
            }
        }

        public static void InactivateAllJobAssignmentsForBot(string botID)
        {
            if (!botJobAssignments.ContainsKey(botID))
            {
                return;
            }

            foreach (BotJobAssignment assignment in botJobAssignments[botID])
            {
                assignment.Inactivate();
            }
        }

        public static int NumberOfConsecutiveFailedAssignments(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return 0;
            }

            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Reverse<BotJobAssignment>()
                .TakeWhile(a => a.Status == JobAssignmentStatus.Failed);

            return matchingAssignments.Count();
        }

        public static int NumberOfActiveBots(this Quest quest)
        {
            int num = 0;
            foreach (string id in botJobAssignments.Keys)
            {
                num += botJobAssignments[id]
                    .Where(a => a.Status == JobAssignmentStatus.Active)
                    .Where(a => a.QuestAssignment == quest)
                    .Count();
            }

            //LoggingController.LogInfo("Bots doing " + quest.ToString() + ": " + num);

            return num;
        }

        public static IEnumerable<QuestObjective> RemainingObjectivesForBot(this Quest quest, BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Bot is null", nameof(bot));
            }

            if (quest == null)
            {
                throw new ArgumentNullException("Quest is null", nameof(quest));
            }

            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return quest.AllObjectives;
            }

            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestAssignment == quest)
                .Where(a => a.Status != JobAssignmentStatus.Archived);
            
            return quest.AllObjectives.Where(o => !matchingAssignments.Any(a => a.QuestObjectiveAssignment == o));
        }

        public static QuestObjective NearestToBot(this IEnumerable<QuestObjective> objectives, BotOwner bot)
        {
            Dictionary<QuestObjective, float> objectiveDistances = new Dictionary<QuestObjective, float>();
            foreach (QuestObjective objective in objectives)
            {
                Vector3? firstStepPosition = objective.GetFirstStepPosition();
                if (!firstStepPosition.HasValue)
                {
                    continue;
                }

                objectiveDistances.Add(objective, Vector3.Distance(bot.Position, firstStepPosition.Value));
            }

            if (objectiveDistances.Count == 0)
            {
                return null;
            }

            return objectiveDistances.OrderBy(i => i.Value).First().Key;
        }

        public static DateTime? TimeWhenLastEndedForBot(this Quest quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            // Find all of the bot's assignments with this quest that have not been archived yet
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestAssignment == quest)
                .Where(a => a.Status != JobAssignmentStatus.Archived)
                .Reverse<BotJobAssignment>()
                .SkipWhile(a => !a.EndTime.HasValue);

            if (!matchingAssignments.Any())
            {
                return null;
            }

            return matchingAssignments.First().EndTime;
        }

        public static double? ElapsedTimeWhenLastEndedForBot(this Quest quest, BotOwner bot)
        {
            DateTime? lastObjectiveEndingTime = quest.TimeWhenLastEndedForBot(bot);
            if (!lastObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - lastObjectiveEndingTime.Value).TotalSeconds;
        }

        public static DateTime? TimeWhenBotStarted(this Quest quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            // If the bot is currently doing this quest, find the time it first started
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Reverse<BotJobAssignment>()
                .TakeWhile(a => a.QuestAssignment == quest);

            if (!matchingAssignments.Any())
            {
                return null;
            }

            return matchingAssignments.Last().EndTime;
        }

        public static double? ElapsedTimeSinceBotStarted(this Quest quest, BotOwner bot)
        {
            DateTime? firstObjectiveEndingTime = quest.TimeWhenBotStarted(bot);
            if (!firstObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - firstObjectiveEndingTime.Value).TotalSeconds;
        }

        public static bool CanAssignToBot(this Quest quest, BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Bot is null", nameof(bot));
            }

            if (quest == null)
            {
                throw new ArgumentNullException("Quest is null", nameof(quest));
            }

            // Check if the bot is eligible to do the quest
            if (!quest.CanAssignBot(bot))
            {
                //LoggingController.LogInfo("Cannot assign " + bot.GetText() + " to quest " + quest.ToString());
                return false;
            }

            // If the bot has never been assigned a job, it should be able to do the quest
            // TO DO: Could this return a false positive? 
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return true;
            }

            // Ensure the bot can do at least one of the objectives
            if (!quest.AllObjectives.Any(o => o.CanAssignBot(bot)))
            {
                //LoggingController.LogInfo("Cannot assign " + bot.GetText() + " to any objectives in quest " + quest.ToString());
                return false;
            }

            if (quest.HasBotBeingDoingQuestTooLong(bot, out double? timeDoingQuest))
            {
                return false;
            }

            // Check if at least one of the quest objectives has not been assigned to the bot
            if (quest.RemainingObjectivesForBot(bot).Count() > 0)
            {
                return true;
            }

            // Check if enough time has elasped from the bot's last assignment in the quest
            if (quest.TryArchiveIfBotCanRepeat(bot))
            {
                return true;
            }

            return false;
        }

        public static bool TryArchiveIfBotCanRepeat(this Quest quest, BotOwner bot)
        {
            if (!quest.IsRepeatable)
            {
                return false;
            }

            double? timeSinceQuestEnded = quest.ElapsedTimeWhenLastEndedForBot(bot);
            if (timeSinceQuestEnded.HasValue && (timeSinceQuestEnded >= ConfigController.Config.Questing.BotQuestingRequirements.RepeatQuestDelay))
            {
                LoggingController.LogInfo(bot.GetText() + " is now allowed to repeat quest " + quest.ToString());

                IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                    .Where(a => a.QuestAssignment == quest);

                foreach (BotJobAssignment assignment in matchingAssignments)
                {
                    assignment.Archive();
                }

                return true;
            }

            return false;
        }

        public static int TryArchiveRepeatableAssignments(this BotOwner bot)
        {
            BotJobAssignment[] matchingAssignments = botJobAssignments[bot.Profile.Id]
                    .Where(a => a.QuestAssignment.IsRepeatable)
                    .Where(a => a.Status == JobAssignmentStatus.Completed)
                    .ToArray();

            matchingAssignments.ExecuteForEach(a => a.Archive());

            return matchingAssignments.Length;
        }

        public static bool CanBotRepeatQuestObjective(this QuestObjective objective, BotOwner bot)
        {
            IEnumerable<BotJobAssignment> matchingAssignments = botJobAssignments[bot.Profile.Id]
                .Where(a => a.QuestObjectiveAssignment == objective);

            if (!matchingAssignments.Any())
            {
                return true;
            }
            
            // If the assignment hasn't been archived yet, not enough time has elapsed to repeat it
            if (!objective.IsRepeatable && matchingAssignments.Any(a => a.Status == JobAssignmentStatus.Completed))
            {
                return false;
            }

            return objective.IsRepeatable && matchingAssignments.All(a => a.Status == JobAssignmentStatus.Archived);
        }

        public static bool HasBotBeingDoingQuestTooLong(this Quest quest, BotOwner bot, out double? time)
        {
            time = quest.ElapsedTimeSinceBotStarted(bot);
            if (time.HasValue && (time >= ConfigController.Config.Questing.BotQuestingRequirements.MaxTimePerQuest))
            {
                return true;
            }

            return false;
        }

        public static BotJobAssignment GetCurrentJobAssignment(this BotOwner bot, bool allowUpdate = true)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                botJobAssignments.Add(bot.Profile.Id, new List<BotJobAssignment>());
            }

            if (allowUpdate && DoesBotHaveNewJobAssignment(bot))
            {
                LoggingController.LogInfo("Bot " + bot.GetText() + " is now doing " + botJobAssignments[bot.Profile.Id].Last().ToString());

                if (botJobAssignments[bot.Profile.Id].Count > 1)
                {
                    BotJobAssignment lastAssignment = botJobAssignments[bot.Profile.Id].TakeLast(2).First();

                    LoggingController.LogInfo("Bot " + bot.GetText() + " was previously doing " + lastAssignment.ToString());

                    double? timeSinceBotStartedQuest = lastAssignment.QuestAssignment.ElapsedTimeSinceBotStarted(bot);
                    double? timeSinceBotLastFinishedQuest = lastAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(bot);

                    string startedTimeText = timeSinceBotStartedQuest.HasValue ? timeSinceBotStartedQuest.Value.ToString() : "N/A";
                    string lastFinishedTimeText = timeSinceBotLastFinishedQuest.HasValue ? timeSinceBotLastFinishedQuest.Value.ToString() : "N/A";
                    LoggingController.LogInfo("Time since first objective ended: " + startedTimeText + ", Time since last objective ended: " + lastFinishedTimeText);
                }
            }

            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                return botJobAssignments[bot.Profile.Id].Last();
            }

            if (allowUpdate)
            {
                LoggingController.LogWarning("Could not get a job assignment for bot " + bot.GetText());
            }

            return null;
        }

        public static bool DoesBotHaveNewJobAssignment(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                botJobAssignments.Add(bot.Profile.Id, new List<BotJobAssignment>());
            }

            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                BotJobAssignment currentAssignment = botJobAssignments[bot.Profile.Id].Last();
                
                // Check if the bot is currently doing an assignment
                if (currentAssignment.IsActive)
                {
                    return false;
                }

                // Check if more steps are available for the bot's current assignment
                if (currentAssignment.TrySetNextObjectiveStep(false))
                {
                    return true;
                }

                //LoggingController.LogInfo("There are no more steps available for " + bot.GetText() + " in " + (currentAssignment.QuestObjectiveAssignment?.ToString() ?? "???"));
            }

            if (bot.GetNewBotJobAssignment() != null)
            {
                return true;
            }

            return false;
        }

        public static BotJobAssignment GetNewBotJobAssignment(this BotOwner bot)
        {
            // Do not select another quest objective if the bot wants to extract
            BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);
            if (botObjectiveManager?.DoesBotWantToExtract() == true)
            {
                return null;
            }

            float? distanceToExfilPoint = botObjectiveManager?.DistanceToInitialExfiltrationPoint();
            float minDistanceToSwitchExfil = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetMaxExfilPointDistance() * ConfigController.Config.Questing.BotQuests.ExfilReachedMinFraction;
            if (distanceToExfilPoint.HasValue && (distanceToExfilPoint.Value < minDistanceToSwitchExfil))
            {
                botObjectiveManager?.SetExfiliationPoint();
            }

            // Get the bot's most recent assingment if applicable
            Quest quest = null;
            QuestObjective objective = null;
            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                quest = botJobAssignments[bot.Profile.Id].Last().QuestAssignment;
                objective = botJobAssignments[bot.Profile.Id].Last().QuestObjectiveAssignment;
            }

            if (quest?.HasBotBeingDoingQuestTooLong(bot, out double? timeDoingQuest) == true)
            {
                LoggingController.LogInfo(bot.GetText() + " has been performing quest " + quest.ToString() + " for " + timeDoingQuest.Value + "s and will get a new one.");
                quest = null;
                objective = null;
            }

            // Try to find a quest that has at least one objective that can be assigned to the bot
            List<Quest> invalidQuests = new List<Quest>();
            Stopwatch timeoutMonitor = Stopwatch.StartNew();
            do
            {
                // Find the nearest objective for the bot's currently assigned quest (if any)
                objective = quest?
                    .RemainingObjectivesForBot(bot)?
                    .Where(o => o.CanAssignBot(bot))?
                    .Where(o => o.CanBotRepeatQuestObjective(bot))?
                    .NearestToBot(bot);
                
                // Exit the loop if an objective was found for the bot
                if (objective != null)
                {
                    break;
                }
                if (quest != null)
                {
                    LoggingController.LogInfo(bot.GetText() + " cannot select quest " + quest.ToString() + " because it has no valid objectives");
                    invalidQuests.Add(quest);
                }

                // If no objectives were found, select another quest
                quest = bot.GetRandomQuest(invalidQuests);

                // If a quest hasn't been found within a certain amount of time, something is wrong
                if (timeoutMonitor.ElapsedMilliseconds > ConfigController.Config.Questing.QuestSelectionTimeout)
                {
                    if (bot.TryArchiveRepeatableAssignments() > 0)
                    {
                        LoggingController.LogWarning(bot.GetText() + " cannot select any quests. Trying to select a repeatable quest early instead...");
                        continue;
                    }

                    LoggingController.LogError(bot.GetText() + " could not select any of the following quests: " + string.Join(", ", bot.GetAllPossibleQuests()));
                    botObjectiveManager?.StopQuesting();

                    if (botObjectiveManager?.BotMonitor?.TryInstructBotToExtract() == true)
                    {
                        LoggingController.LogWarning(bot.GetText() + " cannot select any quests. Extracting instead...");
                        return null;
                    }

                    throw new TimeoutException("Finding a quest for " + bot.GetText() + " took too long. Questing disabled.");
                }

            } while (objective == null);

            // Once a valid assignment is selected, assign it to the bot
            BotJobAssignment assignment = new BotJobAssignment(bot, quest, objective);
            botJobAssignments[bot.Profile.Id].Add(assignment);
            return assignment;
        }

        public static IEnumerable<Quest> GetAllPossibleQuests(this BotOwner bot)
        {
            int botGroupSize = BotLogic.HiveMind.BotHiveMindMonitor.GetFollowers(bot).Count + 1;

            return allQuests
                .Where(q => q.NumberOfValidObjectives > 0)
                .Where(q => q.MaxBotsInGroup >= botGroupSize)
                .Where(q => q.CanMoreBotsDoQuest())
                .Where(q => q.CanAssignToBot(bot))
                .Where(q => q.Desirability != 0)
                .ToArray();
        }

        public static Quest GetRandomQuest(this BotOwner bot, IEnumerable<Quest> invalidQuests)
        {
            //return GetRandomQuest_OLD(bot, invalidQuests);

            Stopwatch questSelectionTimer = Stopwatch.StartNew();

            Quest[] assignableQuests = bot.GetAllPossibleQuests()
                .Where(q => !invalidQuests.Contains(q))
                .ToArray();

            foreach (Quest quest in assignableQuests)
            {
                if (quest.Desirability != -1)
                {
                    continue;
                }

                quest.Desirability = 100 * (1f - quest.Priority / 25f) * (quest.ChanceForSelecting / 100);
                LoggingController.LogWarning("Quest " + quest.ToString() + " Desirability set to " + quest.Desirability + ". Priority=" + quest.Priority + ", ChanceForSelecting=" + quest.ChanceForSelecting);
            }

            if (!assignableQuests.Any())
            {
                return null;
            }

            BotObjectiveManager botObjectiveManager = BotObjectiveManager.GetObjectiveManagerForBot(bot);
            Vector3? vectorToExfil = botObjectiveManager?.VectorToInitialExfiltrationPoint();

            Dictionary<Quest, Configuration.MinMaxConfig> questDistanceRanges = new Dictionary<Quest, Configuration.MinMaxConfig>();
            Dictionary<Quest, Configuration.MinMaxConfig> questExfilAngleRanges = new Dictionary<Quest, Configuration.MinMaxConfig>();
            foreach (Quest quest in assignableQuests)
            {
                IEnumerable<Vector3?> objectivePositions = quest.ValidObjectives.Select(o => o.GetFirstStepPosition());
                IEnumerable<Vector3> validObjectivePositions = objectivePositions.Where(p => p.HasValue).Select(p => p.Value);
                IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(bot.Position, p));

                questDistanceRanges.Add(quest, new Configuration.MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));

                if (vectorToExfil.HasValue)
                {
                    IEnumerable<Vector3> vectorsToObjectivePositions = validObjectivePositions.Select(p => p - bot.Position);
                    IEnumerable<float> anglesToObjectives = vectorsToObjectivePositions.Select(p => Vector3.Angle(p - bot.Position, vectorToExfil.Value));

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
            int maxRandomDistance = (int)Math.Ceiling(maxDistance * ConfigController.Config.Questing.BotQuests.DistanceRandomness / 100.0);
            float maxExfilAngle = ConfigController.Config.Questing.BotQuests.ExfilDirectionMaxAngle;

            int distanceRandomness = ConfigController.Config.Questing.BotQuests.DistanceRandomness;
            int desirabilityRandomness = ConfigController.Config.Questing.BotQuests.DesirabilityRandomness;

            float distanceWeighting = ConfigController.Config.Questing.BotQuests.DistanceWeighting;
            float desirabilityWeighting = ConfigController.Config.Questing.BotQuests.DesirabilityWeighting;
            float exfilDirectionWeighting = ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting["default"];
            if (ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey(Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id))
            {
                exfilDirectionWeighting = ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting[Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id];
            }

            System.Random random = new System.Random();
            Dictionary<Quest, double> questDistanceFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o => 1 - (o.Value.Min + random.Next(-1 * maxRandomDistance, maxRandomDistance)) / maxDistance);
            Dictionary<Quest, float> questDesirabilityFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o => (o.Key.Desirability + random.Next(-1 * desirabilityRandomness, desirabilityRandomness)) / 100);
            Dictionary<Quest, double> questExfilAngleFactor = questExfilAngleRanges
                .ToDictionary(o => o.Key, o => Math.Max(0, o.Value.Min - maxExfilAngle) / (180 - maxExfilAngle));

            IEnumerable<Quest> sortedQuests = questDistanceRanges
                .OrderBy
                (o =>
                    (questDistanceFractions[o.Key] * distanceWeighting)
                    + (questDesirabilityFractions[o.Key] * desirabilityWeighting)
                    - (questExfilAngleFactor[o.Key] * exfilDirectionWeighting)
                )
                .Select(o => o.Key);

            Quest selectedQuest = sortedQuests.Last();

            LoggingController.LogInfo("Distance: " + questDistanceFractions[selectedQuest] + ", Desirability: " + questDesirabilityFractions[selectedQuest] + ", Exfil Angle Factor: " + questExfilAngleFactor[selectedQuest]);
            //LoggingController.LogInfo("Time for quest selection: " + questSelectionTimer.ElapsedMilliseconds + "ms");

            return selectedQuest;
        }

        public static Quest GetRandomQuest_OLD(this BotOwner bot, IEnumerable<Quest> invalidQuests)
        {
            // Group all valid quests by their priority number in ascending order
            var groupedQuests = allQuests
                .Where(q => !invalidQuests.Contains(q))
                .Where(q => q.NumberOfValidObjectives > 0)
                .Where(q => q.CanMoreBotsDoQuest())
                .Where(q => q.CanAssignToBot(bot))
                .GroupBy
                (
                    q => q.Priority,
                    q => q,
                    (key, q) => new { Priority = key, Quests = q.ToList() }
                )
                .OrderBy(g => g.Priority);

            if (!groupedQuests.Any())
            {
                return null;
            }

            foreach (var priorityGroup in groupedQuests)
            {
                // Get the distances to the nearest and furthest objectives for each quest in the group
                Dictionary<Quest, Configuration.MinMaxConfig> questObjectiveDistances = new Dictionary<Quest, Configuration.MinMaxConfig>();
                foreach (Quest quest in priorityGroup.Quests)
                {
                    IEnumerable<Vector3?> objectivePositions = quest.ValidObjectives.Select(o => o.GetFirstStepPosition());
                    IEnumerable<Vector3> validObjectivePositions = objectivePositions.Where(p => p.HasValue).Select(p => p.Value);
                    IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(bot.Position, p));

                    questObjectiveDistances.Add(quest, new Configuration.MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));
                }

                if (questObjectiveDistances.Count == 0)
                {
                    continue;
                }

                // Calculate the maximum amount of "randomness" to apply to each quest
                double distanceRange = questObjectiveDistances.Max(q => q.Value.Max) - questObjectiveDistances.Min(q => q.Value.Min);
                int maxRandomDistance = (int)Math.Ceiling(distanceRange * ConfigController.Config.Questing.BotQuests.DistanceRandomness / 100.0);

                //string timestampText = "[" + DateTime.Now.ToLongTimeString() + "] ";
                //LoggingController.LogInfo(timestampText + "Possible quests for priority " + priorityGroup.Priority + ": " + questObjectiveDistances.Count + ", Distance Range: " + distanceRange);
                //LoggingController.LogInfo(timestampText + "Possible quests for priority " + priorityGroup.Priority + ": " + string.Join(", ", questObjectiveDistances.Select(o => o.Key.Name)));

                // Sort the quests in the group by their distance to you, with some randomness applied, in ascending order
                System.Random random = new System.Random();
                IEnumerable<Quest> randomizedQuests = questObjectiveDistances
                    .OrderBy(q => q.Value.Min + random.Next(-1 * maxRandomDistance, maxRandomDistance))
                    .Select(q => q.Key);

                // Use a random number to determine if the bot should be assigned to the first quest in the list
                Quest firstRandomQuest = randomizedQuests.First();
                if (random.Next(1, 100) <= firstRandomQuest.ChanceForSelecting)
                {
                    return firstRandomQuest;
                }
            }

            // If no quest was assigned to the bot, randomly assign a quest in the first priority group as a fallback method
            return groupedQuests.First().Quests.Random();
        }

        public static IEnumerable<BotJobAssignment> GetCompletedOrAchivedQuests(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return Enumerable.Empty<BotJobAssignment>();
            }

            return botJobAssignments[bot.Profile.Id].Where(a => a.IsCompletedOrArchived);
        }

        public static int NumberOfCompletedOrAchivedQuests(this BotOwner bot)
        {
            IEnumerable<BotJobAssignment> assignments = bot.GetCompletedOrAchivedQuests();
            
            return assignments
                .Distinct(a => a.QuestAssignment)
                .Count();
        }

        public static int NumberOfCompletedOrAchivedEFTQuests(this BotOwner bot)
        {
            IEnumerable<BotJobAssignment> assignments = bot.GetCompletedOrAchivedQuests();
            
            return assignments
                .Distinct(a => a.QuestAssignment)
                .Where(a => a.QuestAssignment.IsEFTQuest)
                .Count();
        }

        public static void WriteQuestLogFile(long timestamp)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Writing quest log file...");

            if (allQuests.Count == 0)
            {
                LoggingController.LogWarning("No quests to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Quest Name,Objective,Steps,Min Level,Max Level,First Step Position");

            // Write a row for every objective in every quest
            foreach (Quest quest in allQuests)
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();
                    if (!firstPosition.HasValue)
                    {
                        continue;
                    }

                    sb.Append(quest.Name.Replace(",", "") + ",");
                    sb.Append("\"" + objective.ToString().Replace(",", "") + "\",");
                    sb.Append(objective.StepCount + ",");
                    sb.Append(quest.MinLevel + ",");
                    sb.Append(quest.MaxLevel + ",");
                    sb.AppendLine((firstPosition.HasValue ? "\"" + firstPosition.Value.ToString() + "\"" : "N/A"));
                }
            }

            string filename = ConfigController.GetLoggingPath()
                + Components.BotQuestBuilder.PreviousLocationID.Replace(" ", "")
                + "_"
                + timestamp
                + "_quests.csv";
            
            LoggingController.CreateLogFile("quest", filename, sb.ToString());
        }

        public static void WriteBotJobAssignmentLogFile(long timestamp)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Writing bot job assignment log file...");

            if (botJobAssignments.Count == 0)
            {
                LoggingController.LogWarning("No bot job assignments to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bot Name,Bot Nickname,Bot Level,Assignment Status,Quest Name,Objective Name,Step Number,Start Time,End Time");

            // Write a row for every quest, objective, and step that each bot was assigned to perform
            foreach (string botID in botJobAssignments.Keys)
            {
                foreach (BotJobAssignment assignment in botJobAssignments[botID])
                {
                    sb.Append(assignment.BotName + ",");
                    sb.Append("\"" + assignment.BotNickname.Replace(",", "") + "\",");
                    sb.Append(assignment.BotLevel + ",");
                    sb.Append(assignment.Status.ToString() + ",");
                    sb.Append("\"" + (assignment.QuestAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveStepAssignment?.StepNumber?.ToString() ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.StartTime?.ToLongTimeString() ?? "N/A") + "\",");
                    sb.AppendLine("\"" + (assignment.EndTime?.ToLongTimeString() ?? "N/A") + "\",");
                }
            }

            string filename = ConfigController.GetLoggingPath()
                + Components.BotQuestBuilder.PreviousLocationID.Replace(" ", "")
                + "_"
                + timestamp
                + "_assignments.csv";

            LoggingController.CreateLogFile("bot job assignment", filename, sb.ToString());
        }

        public static IEnumerable<JobAssignment> CreateAllPossibleJobAssignments()
        {
            List<JobAssignment> allAssignments = new List<JobAssignment>();

            foreach (Quest quest in allQuests)
            {
                foreach (QuestObjective objective in quest.ValidObjectives)
                {
                    foreach (QuestObjectiveStep step in objective.AllSteps)
                    {
                        JobAssignment assignment = new JobAssignment(quest, objective, step);
                        allAssignments.Add(assignment);
                    }
                }
            }

            return allAssignments;
        }
    }
}
