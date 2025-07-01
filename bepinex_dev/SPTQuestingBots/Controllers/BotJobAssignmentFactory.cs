using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using SPTQuestingBots.BotLogic.Objective;
using SPTQuestingBots.Components;
using SPTQuestingBots.Models.Questing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

            if (quest.IsCamping && (ConfigController.Config.Questing.BotQuests.DesirabilityCampingMultiplier != 1))
            {
                float newDesirability = quest.Desirability * ConfigController.Config.Questing.BotQuests.DesirabilityCampingMultiplier;

                LoggingController.LogInfo("Adjusting desirability of camping quest " + quest.ToString() + " from " + quest.Desirability + " to " + newDesirability);

                quest.Desirability = newDesirability;
            }

            if (quest.IsSniping && (ConfigController.Config.Questing.BotQuests.DesirabilitySnipingMultiplier != 1))
            {
                float newDesirability = quest.Desirability * ConfigController.Config.Questing.BotQuests.DesirabilitySnipingMultiplier;

                LoggingController.LogInfo("Adjusting desirability of sniping quest " + quest.ToString() + " from " + quest.Desirability + " to " + newDesirability);

                quest.Desirability = newDesirability;
            }

            allQuests.Add(quest);
        }

        public static Quest FindQuest(string questID)
        {
            IEnumerable<Quest> matchingQuests = allQuests.Where(q => q.Template.Id == questID);
            if (matchingQuests.Count() == 1)
            {
                return matchingQuests.First();
            }

            return null;
        }

        public static void RemoveBlacklistedQuestObjectives(string locationId)
        {
            foreach (Quest quest in allQuests.ToArray())
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    // Check if Lightkeeper Island quests should be blacklisted
                    if (locationId == "Lighthouse")
                    {
                        bool visitsIsland = objective.GetAllPositions()
                            .Where(p => p.HasValue)
                            .Any(position => Singleton<GameWorld>.Instance.GetComponent<LocationData>().IsPointOnLightkeeperIsland(position.Value));

                        if (visitsIsland && !ConfigController.Config.Questing.BotQuests.LightkeeperIslandQuests.Enabled)
                        {
                            if (quest.TryRemoveObjective(objective))
                            {
                                LoggingController.LogInfo("Removing quest objective on Lightkeeper island: " + objective + " for quest " + quest);
                            }
                            else
                            {
                                LoggingController.LogError("Could not remove quest objective on Lightkeeper island: " + objective + " for quest " + quest);
                            }
                        }
                    }

                    // https://github.com/dwesterwick/SPTQuestingBots/issues/18
                    // Disable quests that try to go to the Scav Island, pathing is broken there
                    if (locationId == "Shoreline")
                    {
                        bool visitsIsland = objective.GetAllPositions()
                            .Where(p => p.HasValue)
                            .Any(position => position.Value.x > 160 && position.Value.z > 360);
                        
                        if (visitsIsland)
                        {
                            if (quest.TryRemoveObjective(objective))
                            {
                                LoggingController.LogInfo("Removing quest objective on Scav island: " + objective + " for quest " + quest);
                            }
                            else
                            {
                                LoggingController.LogError("Could not remove quest objective on Scav island: " + objective + " for quest " + quest);
                            }
                        }
                    }

                    // If there are no remaining objectives, remove the quest too
                    if (quest.NumberOfObjectives == 0)
                    {
                        LoggingController.LogInfo("Removing quest with no valid objectives: " + quest + "...");
                        allQuests.Remove(quest);
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
            float pendingTimeLimit = 0.3f;

            int num = 0;
            foreach (string id in botJobAssignments.Keys)
            {
                num += botJobAssignments[id]
                    .Where(a => a.StartTime.HasValue)
                    .Where(a => (a.Status == JobAssignmentStatus.Active) || ((a.Status == JobAssignmentStatus.Pending) && (a.TimeSinceStarted().Value < pendingTimeLimit)))
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
                    LoggingController.LogDebug("Bot " + bot.GetText() + " was previously doing " + lastAssignment.ToString());

                    //double? timeSinceBotStartedQuest = lastAssignment.QuestAssignment.ElapsedTimeSinceBotStarted(bot);
                    //double? timeSinceBotLastFinishedQuest = lastAssignment.QuestAssignment.ElapsedTimeWhenLastEndedForBot(bot);
                    //string startedTimeText = timeSinceBotStartedQuest.HasValue ? timeSinceBotStartedQuest.Value.ToString() : "N/A";
                    //string lastFinishedTimeText = timeSinceBotLastFinishedQuest.HasValue ? timeSinceBotLastFinishedQuest.Value.ToString() : "N/A";
                    //LoggingController.LogInfo("Time since first objective ended: " + startedTimeText + ", Time since last objective ended: " + lastFinishedTimeText);
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
            BotObjectiveManager botObjectiveManager = bot.GetObjectiveManager();
            if (botObjectiveManager?.DoesBotWantToExtract() == true)
            {
                return null;
            }

            float maxDistanceBetweenExfils = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().GetMaxDistanceBetweenExfils();
            float minDistanceToSwitchExfil = maxDistanceBetweenExfils * ConfigController.Config.Questing.BotQuests.ExfilReachedMinFraction;

            // If the bot is close to its selected exfil (only used for quest selection), select a new one
            float? distanceToExfilPoint = botObjectiveManager?.DistanceToExfiltrationPointForQuesting();
            if (distanceToExfilPoint.HasValue && (distanceToExfilPoint.Value < minDistanceToSwitchExfil))
            {
                botObjectiveManager?.SetExfiliationPointForQuesting();
            }

            // Get the bot's most recent assingment if applicable
            Quest quest = null;
            QuestObjective objective = null;
            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                quest = botJobAssignments[bot.Profile.Id].Last().QuestAssignment;
                objective = botJobAssignments[bot.Profile.Id].Last().QuestObjectiveAssignment;
            }

            // Clear the bot's assignment if it's been doing the same quest for too long
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
                    //LoggingController.LogInfo(bot.GetText() + " cannot select quest " + quest.ToString() + " because it has no valid objectives");
                    invalidQuests.Add(quest);
                }

                // If no objectives were found, select another quest
                quest = bot.GetRandomQuest(invalidQuests);

                // If a quest hasn't been found within a certain amount of time, something is wrong
                if (timeoutMonitor.ElapsedMilliseconds > ConfigController.Config.Questing.QuestSelectionTimeout)
                {
                    // First try allowing the bot to repeat quests it already completed
                    if (bot.TryArchiveRepeatableAssignments() > 0)
                    {
                        LoggingController.LogWarning(bot.GetText() + " cannot select any quests. Trying to select a repeatable quest early instead...");
                        continue;
                    }

                    // If there are still no quests available for the bot to select, give up trying to select one
                    LoggingController.LogError(bot.GetText() + " could not select any of the following quests: " + string.Join(", ", bot.GetAllPossibleQuests()));
                    botObjectiveManager?.StopQuesting();

                    // Try making the bot extract because it has nothing to do
                    if (botObjectiveManager?.BotMonitor?.GetMonitor<BotExtractMonitor>()?.TryInstructBotToExtract() == true)
                    {
                        LoggingController.LogWarning(bot.GetText() + " cannot select any quests. Extracting instead...");
                        return null;
                    }

                    LoggingController.LogError(bot.GetText() + " cannot select any quests. Questing disabled.");
                    return null;
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
                .Where(q => q.Desirability != 0)
                .Where(q => q.NumberOfValidObjectives > 0)
                .Where(q => q.MaxBotsInGroup >= botGroupSize)
                .Where(q => q.CanMoreBotsDoQuest())
                .Where(q => q.CanAssignToBot(bot))
                .ToArray();
        }

        public static Quest GetRandomQuest(this BotOwner bot, IEnumerable<Quest> invalidQuests)
        {
            Stopwatch questSelectionTimer = Stopwatch.StartNew();

            Quest[] assignableQuests = bot.GetAllPossibleQuests()
                .Where(q => !invalidQuests.Contains(q))
                .ToArray();

            if (!assignableQuests.Any())
            {
                return null;
            }

            BotObjectiveManager botObjectiveManager = bot.GetObjectiveManager();
            Vector3? vectorToExfil = botObjectiveManager?.VectorToExfiltrationPointForQuesting();

            Dictionary<Quest, Configuration.MinMaxConfig> questDistanceRanges = new Dictionary<Quest, Configuration.MinMaxConfig>();
            Dictionary<Quest, Configuration.MinMaxConfig> questExfilAngleRanges = new Dictionary<Quest, Configuration.MinMaxConfig>();

            // Calculate the distances from the bot to all valid quest objectives and the angles between the vector to the bot's selected
            // exfil (for questing) and the vector to each valid quest objective
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
            float exfilDirectionWeighting = 0;

            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;
            if (ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey(locationId))
            {
                exfilDirectionWeighting = ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting[locationId];
            }
            else if (ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting.ContainsKey("default"))
            {
                exfilDirectionWeighting = ConfigController.Config.Questing.BotQuests.ExfilDirectionWeighting["default"];
            }

            System.Random random = new System.Random();
            Dictionary<Quest, double> questDistanceFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o => 1 - (o.Value.Min + random.Next(-1 * maxRandomDistance, maxRandomDistance)) / maxDistance);
            Dictionary<Quest, float> questDesirabilityFractions = questDistanceRanges
                .ToDictionary(o => o.Key, o => 
                (
                    o.Key.Desirability * (o.Key.IsActiveForPlayer ? ConfigController.Config.Questing.BotQuests.DesirabilityActiveQuestMultiplier : 1)
                    + random.Next(-1 * desirabilityRandomness, desirabilityRandomness)) / 100
                );
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

            //LoggingController.LogInfo("Distance: " + questDistanceFractions[selectedQuest] + ", Desirability: " + questDesirabilityFractions[selectedQuest] + ", Exfil Angle Factor: " + questExfilAngleFactor[selectedQuest]);
            //LoggingController.LogInfo("Time for quest selection: " + questSelectionTimer.ElapsedMilliseconds + "ms");

            return selectedQuest;
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

            LoggingController.LogDebug("Writing quest log file...");

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

            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

            string filename = ConfigController.GetLoggingPath()
                + locationId.Replace(" ", "")
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

            LoggingController.LogDebug("Writing bot job assignment log file...");

            if (botJobAssignments.Count == 0)
            {
                LoggingController.LogWarning("No bot job assignments to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Bot Name,Bot Nickname,Bot Difficulty,Bot Level,Assignment Status,Quest Name,Objective Name,Step Number,Start Time,End Time");

            // Write a row for every quest, objective, and step that each bot was assigned to perform
            foreach (string botID in botJobAssignments.Keys)
            {
                foreach (BotJobAssignment assignment in botJobAssignments[botID])
                {
                    sb.Append(assignment.BotName + ",");
                    sb.Append("\"" + assignment.BotNickname.Replace(",", "") + "\",");
                    sb.Append(assignment.BotOwner.Profile.Info.Settings.BotDifficulty.ToString() + ",");
                    sb.Append(assignment.BotLevel + ",");
                    sb.Append(assignment.Status.ToString() + ",");
                    sb.Append("\"" + (assignment.QuestAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveAssignment?.ToString()?.Replace(",", "") ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.QuestObjectiveStepAssignment?.StepNumber?.ToString() ?? "N/A") + "\",");
                    sb.Append("\"" + (assignment.StartTime?.ToLongTimeString() ?? "N/A") + "\",");
                    sb.AppendLine("\"" + (assignment.EndTime?.ToLongTimeString() ?? "N/A") + "\",");
                }
            }

            foreach (Profile profile in Components.Spawning.BotGenerator.GetAllGeneratedBotProfiles())
            {
                if (botJobAssignments.ContainsKey(profile.Id))
                {
                    continue;
                }

                sb.Append("[Not Spawned]" + ",");
                sb.Append("\"" + profile.Info.Nickname.Replace(",", "") + "\",");
                sb.Append(profile.Info.Settings.BotDifficulty.ToString() + ",");
                sb.Append(profile.Info.Level + ",");
                sb.AppendLine(",,,,,,");
            }

            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

            string filename = ConfigController.GetLoggingPath()
                + locationId.Replace(" ", "")
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

        public static IEnumerable<QuestObjective> GetQuestObjectivesNearPosition(Vector3 position, float distance, bool allowEFTQuests = true)
        {
            List<QuestObjective> nearbyObjectives = new List<QuestObjective>();

            foreach (Quest quest in allQuests)
            {
                if (!allowEFTQuests && quest.IsEFTQuest)
                {
                    continue;
                }

                foreach (QuestObjective objective in quest.ValidObjectives)
                {
                    if (Vector3.Distance(position, objective.GetFirstStepPosition().Value) > distance)
                    {
                        continue;
                    }

                    nearbyObjectives.Add(objective);
                }
            }

            return nearbyObjectives;
        }

        public static void CheckBotJobAssignmentValidity(BotOwner bot)
        {
            BotJobAssignment botJobAssignment = GetCurrentJobAssignment(bot, false);
            if (botJobAssignment?.QuestAssignment == null)
            {
                return;
            }

            int botGroupSize = BotLogic.HiveMind.BotHiveMindMonitor.GetFollowers(bot).Count + 1;
            if (botGroupSize > botJobAssignment.QuestAssignment.MaxBotsInGroup)
            {
                BotObjectiveManager botObjectiveManager = bot.GetObjectiveManager();

                if (botObjectiveManager.TryChangeObjective())
                {
                    LoggingController.LogWarning("Selected new quest for " + bot.GetText() + " because it has too many followers for its previous quest");
                }
                else
                {
                    LoggingController.LogError("Cannot select new quest for " + bot.GetText() + ". It has too many followers for quest " + botJobAssignment.QuestAssignment.ToString());
                }
            }
        }
    }
}
