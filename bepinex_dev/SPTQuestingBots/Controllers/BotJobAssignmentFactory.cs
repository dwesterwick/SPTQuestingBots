using EFT;
using SPTQuestingBots.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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

        public static void Clear()
        {
            // Only remove quests that are not based on an EFT quest template
            allQuests.RemoveAll(q => q.Template == null);

            // Remove all objectives for remaining quests. New objectives will be generated after loading the map.
            foreach (Quest quest in allQuests)
            {
                quest.Clear();
            }
        }

        public static void AddQuest(Quest quest)
        {
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

        public static Quest[] FindQuestsWithZone(string zoneId)
        {
            IEnumerable<Quest> matchingQuests = allQuests.Where(q => q.GetObjectiveForZoneID(zoneId) != null);
            return matchingQuests.ToArray();
        }

        public static IEnumerator ProcessAllQuests(Action<Quest> action)
        {
            // Process each of the quests created by an EFT quest template using the provided action
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action);
        }

        public static IEnumerator ProcessAllQuests<T1>(Action<Quest, T1> action, T1 param1)
        {
            // Process each of the quests created by an EFT quest template using the provided action
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1);
        }

        public static IEnumerator ProcessAllQuests<T1, T2>(Action<Quest, T1, T2> action, T1 param1, T2 param2)
        {
            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(allQuests, action, param1, param2);
        }

        public static Quest GetRandomQuestForBot(BotOwner bot)
        {
            // Group all valid quests by their priority number in ascending order
            var groupedQuests = allQuests
                .Where(q => q.CanAssignBot(bot))
                .Where(q => q.NumberOfValidObjectives > 0)
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
                int maxRandomDistance = (int)Math.Ceiling(distanceRange * ConfigController.Config.BotQuests.DistanceRandomness / 100.0);

                //LoggingController.LogInfo("Possible quests for priority " + priorityGroup.Priority + ": " + questObjectiveDistances.Count + ", Distance Range: " + distanceRange);

                // Sort the quests in the group by their distance to you, with some randomness applied, in ascending order
                System.Random random = new System.Random();
                IEnumerable<Quest> randomizedQuests = questObjectiveDistances
                    .OrderBy(q => q.Value.Min + random.NextFloat(-1 * maxRandomDistance, maxRandomDistance))
                    .Select(q => q.Key);

                // Use a random number to determine if the bot should be assigned to the first quest in the list
                Quest firstRandomQuest = randomizedQuests.First();
                if (random.NextFloat(1, 100) < firstRandomQuest.ChanceForSelecting)
                {
                    return firstRandomQuest;
                }
            }

            // If no quest was assigned to the bot, randomly assign a quest in the first priority group as a fallback method
            return groupedQuests.First().Quests.Random();
        }

        public static void WriteQuestLogFile()
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Writing quest log file...");

            // Write the header row
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Quest Name,Objective,Steps,Min Level,Max Level,First Step Position,Active Bots,Successful Bots,Unsuccessful Bots");

            // Write a row for every objective in every quest
            foreach (Quest quest in allQuests)
            {
                foreach (QuestObjective objective in quest.AllObjectives)
                {
                    Vector3? firstPosition = objective.GetFirstStepPosition();

                    sb.Append(quest.Name + ",");
                    sb.Append("\"" + objective.ToString() + "\",");
                    sb.Append(objective.StepCount + ",");
                    sb.Append(quest.MinLevel + ",");
                    sb.Append(quest.MaxLevel + ",");
                    sb.Append((firstPosition.HasValue ? "\"" + firstPosition.Value.ToString() + "\"" : "N/A") + ",");
                    sb.Append(GetBotListText(objective.ActiveBots) + ",");
                    sb.Append(GetBotListText(objective.SuccessfulBots) + ",");
                    sb.AppendLine(GetBotListText(objective.UnsuccessfulBots));
                }
            }

            string filename = ConfigController.GetLoggingPath()
                + "quests_"
                + BotQuestBuilder.PreviousLocationID.Replace(" ", "")
                + "_"
                + DateTime.Now.ToFileTimeUtc()
                + ".csv";

            try
            {
                if (!Directory.Exists(ConfigController.LoggingPath))
                {
                    Directory.CreateDirectory(ConfigController.LoggingPath);
                }

                File.WriteAllText(filename, sb.ToString());

                LoggingController.LogInfo("Writing quest log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LoggingController.LogError("Writing quest log file...failed!");
                LoggingController.LogError(e.ToString());
            }
        }

        private static string GetBotListText(IEnumerable<BotOwner> bots)
        {
            return string.Join(",", bots.Select(b => b.Profile.Nickname + " (Level " + b.Profile.Info.Level + ")"));
        }
    }
}
