using Comfort.Common;
using Diz.Utils;
using EFT;
using QuestingBots.Components;
using QuestingBots.Components.Spawning;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using QuestingBots.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Controllers
{
    public static class BotJobAssignmentController
    {
        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(Singleton<ConfigUtil>.Instance.CurrentConfig.MaxCalcTimePerFrame);
        private static List<BotQuest> allQuests = new List<BotQuest>();
        private static Dictionary<string, List<BotJobAssignment>> botJobAssignments = new Dictionary<string, List<BotJobAssignment>>();

        public static int QuestCount => allQuests.Count;

        public static BotQuest[] FindQuestsWithZone(string zoneId) => allQuests.Where(q => q.GetObjectiveForZoneID(zoneId) != null).ToArray();
        public static bool CanMoreBotsDoQuest(this BotQuest quest) => quest.NumberOfActiveBots() < quest.MaxBots;

        public static void Clear()
        {
            // Only remove quests that are not based on an EFT quest template
            allQuests.RemoveAll(q => q.Template == null);

            // Remove all objectives for remaining quests. New objectives will be generated after loading the map.
            foreach (BotQuest quest in allQuests)
            {
                quest.Clear();
            }

            botJobAssignments.Clear();
        }

        public static IEnumerator ProcessAllQuests(Action<BotQuest> action)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action);
        }

        public static IEnumerator ProcessAllQuests<T1>(Action<BotQuest, T1> action, T1 param1)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1);
        }

        public static IEnumerator ProcessAllQuests<T1, T2>(Action<BotQuest, T1, T2> action, T1 param1, T2 param2)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1, param2);
        }

        public static void AddQuest(BotQuest quest)
        {
            foreach(BotQuestObjective objective in quest.AllObjectives)
            {
                objective.UpdateQuestObjectiveStepNumbers();
            }

            if (quest.IsCamping && (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityCampingMultiplier != 1))
            {
                float newDesirability = quest.Desirability * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilityCampingMultiplier;

                Singleton<LoggingUtil>.Instance.LogInfo("Adjusting desirability of camping quest " + quest.ToString() + " from " + quest.Desirability + " to " + newDesirability);

                quest.Desirability = newDesirability;
            }

            if (quest.IsSniping && (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilitySnipingMultiplier != 1))
            {
                float newDesirability = quest.Desirability * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.DesirabilitySnipingMultiplier;

                Singleton<LoggingUtil>.Instance.LogInfo("Adjusting desirability of sniping quest " + quest.ToString() + " from " + quest.Desirability + " to " + newDesirability);

                quest.Desirability = newDesirability;
            }

            allQuests.Add(quest);
        }

        public static BotQuest? FindQuest(string questID)
        {
            foreach (BotQuest quest in allQuests)
            {
                if (quest.Template?.Id == questID)
                {
                    return quest;
                }
            }

            return null;
        }

        public static void RemoveBlacklistedQuestObjectives(string locationId)
        {
            foreach (BotQuest quest in allQuests.ToArray())
            {
                foreach (BotQuestObjective objective in quest.AllObjectives)
                {
                    // Check if Lightkeeper Island quests should be blacklisted
                    if (locationId == "Lighthouse")
                    {
                        bool visitsIsland = objective.GetAllPositions()
                            .Where(p => p.HasValue)
                            .Any(position => Singleton<GameWorld>.Instance.GetComponent<LocationData>().IsPointOnLightkeeperIsland(position));

                        if (visitsIsland && !Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuests.LightkeeperIslandQuests.Enabled)
                        {
                            if (quest.TryRemoveObjective(objective))
                            {
                                Singleton<LoggingUtil>.Instance.LogInfo("Removing quest objective on Lightkeeper island: " + objective + " for quest " + quest);
                            }
                            else
                            {
                                Singleton<LoggingUtil>.Instance.LogError("Could not remove quest objective on Lightkeeper island: " + objective + " for quest " + quest);
                            }
                        }
                    }

                    // https://github.com/dwesterwick/QuestingBots/issues/18
                    // Disable quests that try to go to the Scav Island, pathing is broken there
                    if (locationId == "Shoreline")
                    {
                        bool visitsIsland = objective.GetAllPositions()
                            .Where(p => p.HasValue)
                            .Any(position => position!.Value.x > 160 && position.Value.z > 360);
                        
                        if (visitsIsland)
                        {
                            if (quest.TryRemoveObjective(objective))
                            {
                                Singleton<LoggingUtil>.Instance.LogInfo("Removing quest objective on Scav island: " + objective + " for quest " + quest);
                            }
                            else
                            {
                                Singleton<LoggingUtil>.Instance.LogError("Could not remove quest objective on Scav island: " + objective + " for quest " + quest);
                            }
                        }
                    }

                    // If there are no remaining objectives, remove the quest too
                    if (quest.NumberOfObjectives == 0)
                    {
                        Singleton<LoggingUtil>.Instance.LogInfo("Removing quest with no valid objectives: " + quest + "...");
                        allQuests.Remove(quest);
                    }
                }
            }
        }

        public static IEnumerable<BotQuest> GetAllPossibleQuests(this BotOwner bot)
        {
            int botGroupSize = BotLogic.HiveMind.BotHiveMindMonitor.GetFollowers(bot).Count + 1;

            foreach (BotQuest quest in allQuests)
            {
                if (quest.Desirability <= 0)
                {
                    continue;
                }

                if (quest.NumberOfValidObjectives() == 0)
                {
                    continue;
                }

                if (botGroupSize > quest.MaxBotsInGroup)
                {
                    continue;
                }

                if (!quest.CanMoreBotsDoQuest())
                {
                    continue;
                }

                if (!quest.CanAssignToBot(bot))
                {
                    continue;
                }

                yield return quest;
            }
        }

        public static void FailAllJobAssignmentsForBot(string botID)
        {
            if (!botJobAssignments.ContainsKey(botID))
            {
                return;
            }

            foreach (BotJobAssignment assignment in botJobAssignments[botID])
            {
                if (!assignment.IsActive)
                {
                    continue;
                }

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

        public static int NumberOfActiveBots(this BotQuest quest)
        {
            int num = 0;
            foreach (List<BotJobAssignment> botAssignmentList in botJobAssignments.Values)
            {
                foreach (BotJobAssignment assignment in botAssignmentList)
                {
                    if (assignment.QuestAssignment != quest)
                    {
                        continue;
                    }

                    if (!assignment.IsActiveForAssignedBot())
                    {
                        continue;
                    }

                    num++;
                }
            }

            //Singleton<LoggingUtil>.Instance.LogInfo("Bots doing " + quest.ToString() + ": " + num);
            return num;
        }

        private const float PENDING_STATUS_TIME_LIMIT = 0.3f;
        public static bool IsActiveForAssignedBot(this BotJobAssignment botAssignment)
        {
            if (!botAssignment.IsActive)
            {
                return false;
            }

            if ((botAssignment.Status == JobAssignmentStatus.Pending) && (botAssignment.TimeSinceStarted() >= PENDING_STATUS_TIME_LIMIT))
            {
                return false;
            }

            return true;
        }

        public static IEnumerable<BotQuestObjective> RemainingObjectivesForBot(this BotQuest quest, BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Bot is null", nameof(bot));
            }

            if (quest == null)
            {
                throw new ArgumentNullException("Quest is null", nameof(quest));
            }

            foreach (BotQuestObjective objective in quest.AllObjectives)
            {
                if (!botJobAssignments.ContainsKey(bot.Profile.Id))
                {
                    yield return objective;
                    continue;
                }

                if (botJobAssignments[bot.Profile.Id].HasMatchingUnArchivedAssignment(quest, objective))
                {
                    continue;
                }

                yield return objective;
            }
        }

        public static bool HasMatchingUnArchivedAssignment(this IEnumerable<BotJobAssignment> assignments, BotQuest quest, BotQuestObjective objective)
        {
            foreach (BotJobAssignment assignment in assignments)
            {
                if (assignment.Status == JobAssignmentStatus.Archived)
                {
                    continue;
                }

                if ((assignment.QuestAssignment == quest) && (assignment.QuestObjectiveAssignment == objective))
                {
                    return true;
                }
            }

            return false;
        }

        public static BotQuestObjective? NearestToBot(this IEnumerable<BotQuestObjective> objectives, BotOwner bot)
        {
            BotQuestObjective? nearestObjective = null;
            float nearestObjectiveDistance = float.MaxValue;

            foreach (BotQuestObjective objective in objectives)
            {
                Vector3? firstStepPosition = objective.GetFirstStepPosition();
                if (firstStepPosition == null)
                {
                    continue;
                }

                float objectiveDistance = Vector3.Distance(bot.Position, firstStepPosition.Value);
                if (objectiveDistance < nearestObjectiveDistance)
                {
                    nearestObjective = objective;
                    nearestObjectiveDistance = objectiveDistance;
                }
            }

            return nearestObjective;
        }

        public static DateTime? TimeWhenLastEndedForBot(this BotQuest quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            // Find all of the bot's assignments with this quest that have not been archived yet
            DateTime? endTime = null;
            foreach (BotJobAssignment assignment in botJobAssignments[bot.Profile.Id])
            {
                if (assignment.QuestAssignment != quest)
                {
                    continue;
                }

                if (assignment.Status == JobAssignmentStatus.Archived)
                {
                    continue;
                }

                if (assignment.EndTime == null)
                {
                    continue;
                }

                endTime = assignment.EndTime;
            }

            return endTime;
        }

        public static double? ElapsedTimeWhenLastEndedForBot(this BotQuest quest, BotOwner bot)
        {
            DateTime? lastObjectiveEndingTime = quest.TimeWhenLastEndedForBot(bot);
            if (!lastObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - lastObjectiveEndingTime.Value).TotalSeconds;
        }

        public static DateTime? TimeWhenBotStarted(this BotQuest quest, BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return null;
            }

            DateTime? endTime = null;
            for (int i = botJobAssignments[bot.Profile.Id].Count - 1; i >= 0; i--)
            {
                if (botJobAssignments[bot.Profile.Id][i].QuestAssignment != quest)
                {
                    break;
                }

                endTime = botJobAssignments[bot.Profile.Id][i].EndTime;
            }

            return endTime;
        }

        public static double? ElapsedTimeSinceBotStarted(this BotQuest quest, BotOwner bot)
        {
            DateTime? firstObjectiveEndingTime = quest.TimeWhenBotStarted(bot);
            if (!firstObjectiveEndingTime.HasValue)
            {
                return null;
            }

            return (DateTime.Now - firstObjectiveEndingTime.Value).TotalSeconds;
        }

        public static bool CanAssignToBot(this BotQuest quest, BotOwner bot)
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
                //Singleton<LoggingUtil>.Instance.LogInfo("Cannot assign " + bot.GetText() + " to quest " + quest.ToString());
                return false;
            }

            // Ensure the bot can do at least one of the objectives
            if (!quest.CanAssignAnObjectiveToBot(bot))
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Cannot assign " + bot.GetText() + " to any objectives in quest " + quest.ToString());
                return false;
            }

            if (quest.HasBotBeingDoingQuestTooLong(bot, out double? time) && (time != null))
            {
                //Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " has been doing quest " + quest.ToString() + " for " + time + "s");
                return false;
            }

            // Check if at least one of the quest objectives has not been assigned to the bot
            if (quest.RemainingObjectivesForBot(bot).Count() > 0)
            {
                return true;
            }

            //Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " has no remaining objectives for quest " + quest.ToString());

            // Check if enough time has elasped from the bot's last assignment in the quest
            if (quest.TryArchiveIfBotCanRepeat(bot))
            {
                return true;
            }

            return false;
        }

        public static bool CanAssignAnObjectiveToBot(this BotQuest quest, BotOwner bot)
        {
            foreach (BotQuestObjective objective in quest.AllObjectives)
            {
                if (objective.CanAssignBot(bot))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryArchiveIfBotCanRepeat(this BotQuest quest, BotOwner bot)
        {
            if (!quest.IsRepeatable)
            {
                return false;
            }

            double? timeSinceQuestEnded = quest.ElapsedTimeWhenLastEndedForBot(bot);
            if (!timeSinceQuestEnded.HasValue || (timeSinceQuestEnded < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.RepeatQuestDelay))
            {
                return false;
            }

            Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " is now allowed to repeat quest " + quest.ToString());

            foreach (BotJobAssignment assignment in botJobAssignments[bot.Profile.Id])
            {
                if (assignment.QuestAssignment != quest)
                {
                    continue;
                }

                assignment.Archive();
            }

            return true;
        }

        public static int TryArchiveRepeatableAssignments(this BotOwner bot)
        {
            int archivedQuests = 0;
            foreach (BotJobAssignment assignment in botJobAssignments[bot.Profile.Id])
            {
                if (!assignment.QuestAssignment.IsRepeatable)
                {
                    continue;
                }

                if (assignment.Status != JobAssignmentStatus.Completed)
                {
                    continue;
                }

                assignment.Archive();
                archivedQuests++;
            }

            return archivedQuests;
        }

        public static bool CanBotSelectQuestObjective(this BotQuestObjective objective, BotOwner bot)
        {
            List<BotJobAssignment> botAssignments = botJobAssignments[bot.Profile.Id];
            if (botAssignments.Count == 0)
            {
                return true;
            }

            bool allArchived = true;
            int matchingAssignments = 0;
            foreach (BotJobAssignment assignment in botAssignments)
            {
                if (assignment.QuestObjectiveAssignment != objective)
                {
                    continue;
                }

                if (!objective.IsRepeatable && (assignment.Status == JobAssignmentStatus.Completed))
                {
                    return false;
                }

                allArchived = allArchived & assignment.Status == JobAssignmentStatus.Archived;
                matchingAssignments++;
            }

            return matchingAssignments > 0 ? objective.IsRepeatable && allArchived : true;
        }

        public static bool HasBotBeingDoingQuestTooLong(this BotQuest quest, BotOwner bot, out double? time)
        {
            time = quest.ElapsedTimeSinceBotStarted(bot);
            if (time.HasValue && (time >= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxTimePerQuest))
            {
                return true;
            }

            return false;
        }

        public static ReadOnlyCollection<BotJobAssignment> GetAllBotJobAssignments(this BotOwner bot)
        {
            bot.InitializeBotJobAssignmentsList();
            return botJobAssignments[bot.Profile.Id].AsReadOnly();
        }

        private static void InitializeBotJobAssignmentsList(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                botJobAssignments.Add(bot.Profile.Id, new List<BotJobAssignment>());
            }
        }

        public static BotJobAssignment? GetMostRecentJobAssignment(this BotOwner bot)
        {
            bot.InitializeBotJobAssignmentsList();

            if (botJobAssignments[bot.Profile.Id].Count > 0)
            {
                return botJobAssignments[bot.Profile.Id].Last();
            }

            return null;
        }

        public static void Register(this BotJobAssignment assignment)
        {
            Singleton<LoggingUtil>.Instance.LogDebug("Registered assignment " + assignment.ToString() + " for " + assignment.BotOwner.GetText());

            assignment.BotOwner.InitializeBotJobAssignmentsList();
            botJobAssignments[assignment.BotOwner.Profile.Id].Add(assignment);
        }

        public static IEnumerable<BotJobAssignment> GetAllQuestAssignments(this BotOwner bot)
        {
            if (!botJobAssignments.ContainsKey(bot.Profile.Id))
            {
                return Enumerable.Empty<BotJobAssignment>();
            }

            return botJobAssignments[bot.Profile.Id];
        }

        public static IEnumerable<BotJobAssignment> GetCompletedOrAchivedQuests(this BotOwner bot)
        {
            foreach (BotJobAssignment assignment in bot.GetAllQuestAssignments())
            {
                if (!assignment.IsCompletedOrArchived)
                {
                    continue;
                }

                yield return assignment;
            }
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
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.Enabled)
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Writing quest log file...");

            if (allQuests.Count == 0)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("No quests to log.");
                return;
            }

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Quest Name,Objective,Steps,Min Level,Max Level,First Step Position");

            // Write a row for every objective in every quest
            foreach (BotQuest quest in allQuests)
            {
                foreach (BotQuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();
                    if (!firstPosition.HasValue)
                    {
                        continue;
                    }

                    sb.Append(quest.GetName().Replace(",", "") + ",");
                    sb.Append("\"" + objective.ToString().Replace(",", "") + "\",");
                    sb.Append(objective.StepCount + ",");
                    sb.Append(quest.MinLevel + ",");
                    sb.Append(quest.MaxLevel + ",");
                    sb.AppendLine((firstPosition.HasValue ? "\"" + firstPosition.Value.ToString() + "\"" : "N/A"));
                }
            }

            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

            string filename = Singleton<LoggingUtil>.Instance.LoggingPath
                + locationId.Replace(" ", "")
                + "_"
                + timestamp
                + "_quests.csv";

            Singleton<LoggingUtil>.Instance.CreateLogFile("quest", filename, sb.ToString());
        }

        public static void WriteBotJobAssignmentLogFile(long timestamp)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.Enabled)
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Writing bot job assignment log file...");

            if (botJobAssignments.Count == 0)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("No bot job assignments to log.");
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

            foreach (Profile profile in Singleton<GameWorld>.Instance.GetComponent<BotGenerationManager>().GetAllGeneratedBotProfiles())
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

            string filename = Singleton<LoggingUtil>.Instance.LoggingPath
                + locationId.Replace(" ", "")
                + "_"
                + timestamp
                + "_assignments.csv";

            Singleton<LoggingUtil>.Instance.CreateLogFile("bot job assignment", filename, sb.ToString());
        }

        public static IEnumerable<JobAssignment> CreateAllPossibleJobAssignments()
        {
            List<JobAssignment> allAssignments = new List<JobAssignment>();

            foreach (BotQuest quest in allQuests)
            {
                foreach (BotQuestObjective objective in quest.GetValidObjectives())
                {
                    foreach (BotQuestObjectiveStep step in objective.AllSteps)
                    {
                        JobAssignment assignment = new JobAssignment(quest, objective, step);
                        allAssignments.Add(assignment);
                    }
                }
            }

            return allAssignments;
        }

        public static IEnumerable<BotQuestObjective> GetQuestObjectivesNearPosition(Vector3 position, float distance, bool allowEFTQuests = true)
        {
            List<BotQuestObjective> nearbyObjectives = new List<BotQuestObjective>();

            foreach (BotQuest quest in allQuests)
            {
                if (!allowEFTQuests && quest.IsEFTQuest)
                {
                    continue;
                }

                foreach (BotQuestObjective objective in quest.GetValidObjectives())
                {
                    Vector3? firstStepPosition = objective.GetFirstStepPosition();
                    if (!firstStepPosition.HasValue)
                    {
                        Singleton<LoggingUtil>.Instance.LogError("First step position for " + objective + " in " + quest + " is null");
                        continue;
                    }

                    if (Vector3.Distance(position, firstStepPosition.Value) > distance)
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
            BotObjectiveManager? botObjectiveManager = bot.GetObjectiveManager();
            if (botObjectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError($"Cannot retrieve the objective manager for {bot.GetText()}");
                return;
            }

            if (botObjectiveManager.CurrentAssignment == null)
            {
                return;
            }

            int botGroupSize = BotLogic.HiveMind.BotHiveMindMonitor.GetFollowers(bot).Count + 1;
            if (botGroupSize > botObjectiveManager.CurrentAssignment.QuestAssignment.MaxBotsInGroup)
            {
                if (botObjectiveManager.TryChangeObjective())
                {
                    Singleton<LoggingUtil>.Instance.LogWarning("Selected new quest for " + bot.GetText() + " because it has too many followers for its previous quest");
                }
                else
                {
                    Singleton<LoggingUtil>.Instance.LogError("Cannot select new quest for " + bot.GetText() + ". It has too many followers for quest " + botObjectiveManager.CurrentAssignment.QuestAssignment.ToString());
                }
            }
        }
    }
}
