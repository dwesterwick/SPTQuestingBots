using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aki.Common.Http;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using SPTQuestingBots.BotLogic;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Controllers
{
    public class BotQuestController : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsFindingTriggers { get; private set; } = false;
        public static bool HaveTriggersBeenFound { get; private set; } = false;

        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static List<Quest> allQuests = new List<Quest>();
        private static List<string> zoneIDsInLocation = new List<string>();
        private static List<BotOwner> pmcsInLocation = new List<BotOwner>();
        private static List<BotOwner> bossesInLocation = new List<BotOwner>();
        private static Dictionary<BotOwner, List<BotOwner>> bossFollowersInLocation = new Dictionary<BotOwner, List<BotOwner>>();
        private static string previousLocationID = null;

        public IEnumerator Clear()
        {
            IsClearing = true;

            if (IsFindingTriggers)
            {
                enumeratorWithTimeLimit.Abort();

                CoroutineExtensions.EnumeratorWithTimeLimit conditionWaiter = new CoroutineExtensions.EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsFindingTriggers, nameof(IsFindingTriggers), 3000);

                IsFindingTriggers = false;
            }

            // Only remove quests that are not based on an EFT quest template
            allQuests.RemoveAll(q => q.Template == null);

            // Remove all objectives for remaining quests. New objectives will be generated after loading the map.
            foreach (Quest quest in allQuests)
            {
                quest.Clear();
            }

            pmcsInLocation.Clear();
            bossesInLocation.Clear();
            bossFollowersInLocation.Clear();
            zoneIDsInLocation.Clear();

            HaveTriggersBeenFound = false;

            IsClearing = false;
        }

        private void Update()
        {
            // Wait until data from the previous raid has been erased
            if (IsClearing)
            {
                return;
            }

            if (LocationController.CurrentLocation == null)
            {
                // Write a log file containing all loaded quests, their objectives, and which bots have interacted with them. 
                if (HaveTriggersBeenFound)
                {
                    WriteQuestLogFile();
                }

                StartCoroutine(Clear());
                return;
            }

            if (IsFindingTriggers || HaveTriggersBeenFound)
            {
                return;
            }

            StartCoroutine(LoadAllQuests());

            // Store the name of the current location so it can be used when writing the quest log file. The current location will be null when the log is written.
            previousLocationID = LocationController.CurrentLocation.Id;
        }

        public static BotType GetBotType(BotOwner botOwner)
        {
            if (IsBotAPMC(botOwner))
            {
                return BotType.PMC;
            }
            if (IsBotABoss(botOwner))
            {
                return BotType.Boss;
            }
            if (botOwner.Profile.Side == EPlayerSide.Savage)
            {
                return BotType.Scav;
            }

            return BotType.Undetermined;
        }

        public static void RegisterPMC(BotOwner botOwner)
        {
            if (!pmcsInLocation.Contains(botOwner))
            {
                pmcsInLocation.Add(botOwner);
            }
        }

        public static bool IsBotAPMC(BotOwner botOwner)
        {
            return pmcsInLocation.Contains(botOwner);
        }

        public static void RegisterBoss(BotOwner botOwner)
        {
            if (!bossesInLocation.Contains(botOwner))
            {
                bossesInLocation.Add(botOwner);
            }
        }

        public static bool IsBotABoss(BotOwner botOwner)
        {
            return bossesInLocation.Contains(botOwner);
        }

        public static void RegisterBossFollower(BotOwner boss, BotOwner follower)
        {
            if (bossFollowersInLocation.ContainsKey(boss))
            {
                bossFollowersInLocation[boss].Add(follower);
            }
            else
            {
                bossFollowersInLocation.Add(boss, new List<BotOwner>() { follower });
            }
        }

        public static IReadOnlyCollection<BotOwner> GetAliveFollowers(BotOwner botOwner)
        {
            // Check if the bot has any followers
            if (!bossFollowersInLocation.ContainsKey(botOwner))
            {
                return new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
            }

            BotOwner[] aliveFollowers = bossFollowersInLocation[botOwner]
                .Where(b => (b.BotState == EBotState.Active) && !b.IsDead)
                .ToArray();

            return new ReadOnlyCollection<BotOwner>(aliveFollowers);
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
                Dictionary<Quest, MinMaxConfig> questObjectiveDistances = new Dictionary<Quest, MinMaxConfig>();
                foreach (Quest quest in priorityGroup.Quests)
                {
                    IEnumerable<Vector3?> objectivePositions = quest.ValidObjectives.Select(o => o.GetFirstStepPosition());
                    IEnumerable<Vector3> validObjectivePositions = objectivePositions.Where(p => p.HasValue).Select(p => p.Value);
                    IEnumerable<float> distancesToObjectives = validObjectivePositions.Select(p => Vector3.Distance(bot.Position, p));

                    questObjectiveDistances.Add(quest, new MinMaxConfig(distancesToObjectives.Min(), distancesToObjectives.Max()));
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

        private IEnumerator LoadAllQuests()
        {
            IsFindingTriggers = true;

            try
            {
                if (allQuests.Count == 0)
                {
                    // Create quests based on the EFT quest templates loaded from the server. This may include custom quests added by mods. 
                    RawQuestClass[] allQuestTemplates = ConfigController.GetAllQuestTemplates();

                    foreach (RawQuestClass questTemplate in allQuestTemplates)
                    {
                        Quest quest = new Quest(ConfigController.Config.BotQuests.EFTQuests.Priority, questTemplate);
                        quest.ChanceForSelecting = ConfigController.Config.BotQuests.EFTQuests.Chance;
                        allQuests.Add(quest);
                    }
                }

                // Process each of the quests created by an EFT quest template
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allQuests, LoadQuest);

                IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();
                //IEnumerable<Type> allTriggerTypes = allTriggers.Select(t => t.GetType()).Distinct();
                //LoggingController.LogInfo("Found " + allTriggers.Count() + " triggers of types: " + string.Join(", ", allTriggerTypes));

                // Create quest objectives for all matching trigger objects found in the map
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allTriggers, ProcessTrigger);

                //IEnumerable<LootItem> allLoot = FindObjectsOfType<LootItem>(); <-- this does not work for inactive quest items!
                IEnumerable<LootItem> allItems = Singleton<GameWorld>.Instance.LootItems.Where(i => i.Item != null).Distinct(i => i.TemplateId);

                //IEnumerable<LootItem> allQuestItems = allItems.Where(l => l.Item.QuestItem);
                //LoggingController.LogInfo("Quest items: " + string.Join(", ", allQuestItems.Select(l => l.Item.LocalizedName())));

                // Create quest objectives for all matching quest items found in the map
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allQuests, LocateQuestItems, allItems);

                // Create a quest where the bots wanders to various spawn points around the map. This was implemented as a stop-gap for maps with few other quests.
                Quest spawnPointQuest = LocationController.CreateSpawnPointQuest();
                if (spawnPointQuest != null)
                {
                    allQuests.Add(spawnPointQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for going to random spawn points");
                }

                // Create a quest where initial PMC's can run to your spawn point (not directly to you). 
                Quest spawnRushQuest = LocationController.CreateSpawnRushQuest();
                if (spawnRushQuest != null)
                {
                    allQuests.Add(spawnRushQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for rushing your spawn point");
                }

                LoadCustomQuests();

                HaveTriggersBeenFound = true;
                LoggingController.LogInfo("Finished loading quest data.");
            }
            finally
            {
                IsFindingTriggers = false;
            }
        }

        private void LoadCustomQuests()
        {
            // Load all JSON files for custom quests
            IEnumerable<Quest> customQuests = ConfigController.GetCustomQuests(LocationController.CurrentLocation.Id);
            if (!customQuests.Any())
            {
                return;
            }

            LoggingController.LogInfo("Loading custom quests...");
            foreach (Quest quest in customQuests)
            {
                int objectiveNum = 0;
                foreach (QuestObjective objective in quest.ValidObjectives.ToArray())
                {
                    objectiveNum++;
                    objective.SetName(quest.Name + ": Objective #" + objectiveNum);

                    if (!objective.TrySnapAllStepPositionsToNavMesh())
                    {
                        LoggingController.LogError("Could not find valid NavMesh positions for all steps in objective " + objective.ToString() + " for quest " + quest.Name);

                        // Try to remove any objectives that have any steps that don't have valid NavMesh positions. If this fails, don't allow any bots to
                        // do that objective. 
                        if (!quest.TryRemoveObjective(objective))
                        {
                            LoggingController.LogError("Could not remove objective " + objective.ToString());
                            objective.MaxBots = 0;
                        }
                    }
                }

                // Do not use quests that don't have any valid objectives (using the check above)
                if (!quest.ValidObjectives.Any() || quest.ValidObjectives.All(o => o.MaxBots == 0))
                {
                    LoggingController.LogError("Could not find any objectives with valid NavMesh positions for quest " + quest.Name + ". Disabling quest.");
                    continue;
                }

                allQuests.Add(quest);
            }
            LoggingController.LogInfo("Loading custom quests...found " + customQuests.Count() + " custom quests.");
        }

        private void LocateQuestItems(Quest quest, IEnumerable<LootItem> allLoot)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    // Get the ID of the item used for the quest, if applicable
                    string target = "";
                    ConditionFindItem conditionFindItem = condition as ConditionFindItem;
                    if (conditionFindItem != null)
                    {
                        target = conditionFindItem.target[0];
                    }
                    if (target == "")
                    {
                        continue;
                    }

                    // Check if an objective has already been added for the item. This is to prevent duplicate objectives from being added for
                    // some EFT quests. 
                    QuestObjective objective = quest.GetObjectiveForLootItem(target);
                    if (objective != null)
                    {
                        continue;
                    }

                    // Check if the quest item exists in the map
                    IEnumerable<LootItem> matchingLootItems = allLoot.Where(l => l.TemplateId == target);
                    if (matchingLootItems.Count() == 0)
                    {
                        continue;
                    }

                    // Ensure the matching item in the map is a quest item
                    LootItem item = matchingLootItems.First();
                    if (!item.Item.QuestItem)
                    {
                        continue;
                    }

                    // Get the collider for the quest item
                    Collider itemCollider = item.GetComponent<Collider>();
                    if (itemCollider == null)
                    {
                        LoggingController.LogError("Quest item " + item.Item.LocalizedName() + " has no collider");
                        continue;
                    }

                    // Try to find the nearest NavMesh position next to the quest item.
                    Vector3? navMeshTargetPoint = LocationController.FindNearestNavMeshPosition(itemCollider.bounds.center, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceItem);
                    if (!navMeshTargetPoint.HasValue)
                    {
                        LoggingController.LogError("Cannot find NavMesh point for quest item " + item.Item.LocalizedName());

                        if (ConfigController.Config.Debug.ShowZoneOutlines)
                        {
                            Vector3[] itemPositionOutline = PathRender.GetSpherePoints(item.transform.position, 0.5f, 10);
                            PathVisualizationData itemPositionSphere = new PathVisualizationData("QuestItem_" + item.Item.LocalizedName(), itemPositionOutline, Color.red);
                            PathRender.AddOrUpdatePath(itemPositionSphere);
                        }

                        continue;
                    }

                    // Add an objective for the quest item using the nearest valid NavMesh position to it
                    quest.AddObjective(new QuestItemObjective(item, navMeshTargetPoint.Value));
                    LoggingController.LogInfo("Found " + item.Item.LocalizedName() + " for quest " + quest.Name);
                }
            }
        }

        private void LoadQuest(Quest quest)
        {
            // Enumerate all zones used by the quest (in any of its objectives)
            List<string> zoneIDs = new List<string>();
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(condition));
                }
            }

            //LoggingController.LogInfo("Zone ID's for quest \"" + quest.Name + "\": " + string.Join(",", zoneIDs));
            foreach (string zoneID in zoneIDs)
            {
                // Check if an objective has already been added for the item. This is to prevent duplicate objectives from being added for
                // some EFT quests. 
                if (quest.GetObjectiveForZoneID(zoneID) != null)
                {
                    continue;
                }

                // Add a new objective for the zone
                QuestZoneObjective objective = new QuestZoneObjective(zoneID);
                objective.MaxBots = ConfigController.Config.BotQuests.EFTQuests.MaxBotsPerQuest;
                quest.AddObjective(objective);
            }

            // Calculate the minimum and maximum player levels allowed for selecting the quest
            quest.MinLevel = getMinLevelForQuest(quest);
            double levelRange = ConfigController.InterpolateForFirstCol(ConfigController.Config.BotQuests.EFTQuests.LevelRange, quest.MinLevel);
            quest.MaxLevel = quest.MinLevel + (int)Math.Ceiling(levelRange);

            //LoggingController.LogInfo("Level range for quest \"" + quest.Name + "\": " + quest.MinLevel + "-" + quest.MaxLevel);
        }

        private int getMinLevelForQuest(Quest quest)
        {
            // Be default, use the minimum level set for the quest template
            int minLevel = quest.Template.Level;

            EQuestStatus eQuestStatus = EQuestStatus.AvailableForStart;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    // Check if a condition-check exists for player level. If so, use that value if it's higher than the current minimum level. 
                    ConditionLevel conditionLevel = condition as ConditionLevel;
                    if (conditionLevel != null)
                    {
                        // TO DO: This might be needed to set maximum player levels for quests in the future, but I don't think this exists in EFT right now. 
                        if ((conditionLevel.compareMethod != ECompareMethod.MoreOrEqual) && (conditionLevel.compareMethod != ECompareMethod.More))
                        {
                            continue;
                        }

                        if (conditionLevel.value <= minLevel)
                        {
                            continue;
                        }

                        minLevel = (int)conditionLevel.value;
                    }

                    // Check if another quest must be completed first. If so, use its minimum player level if it's higher than the current minimum level. 
                    ConditionQuest conditionQuest = condition as ConditionQuest;
                    if (conditionQuest != null)
                    {
                        // Find the required quest
                        string preReqQuestID = conditionQuest.target;
                        Quest preReqQuest = allQuests.First(q => q.Template.Id == preReqQuestID);

                        // Get the minimum player level to start that quest
                        int minLevelForPreReqQuest = getMinLevelForQuest(preReqQuest);
                        if (minLevelForPreReqQuest <= minLevel)
                        {
                            continue;
                        }

                        minLevel = minLevelForPreReqQuest;
                    }
                }
            }

            return minLevel;
        }

        private IEnumerable<string> getAllZoneIDsForQuestCondition(Condition condition)
        {
            List<string> zoneIDs = new List<string>();

            ConditionZone conditionZone = condition as ConditionZone;
            if (conditionZone != null)
            {
                zoneIDs.Add(conditionZone.zoneId);
            }

            ConditionLeaveItemAtLocation conditionLeaveItemAtLocation = condition as ConditionLeaveItemAtLocation;
            if (conditionLeaveItemAtLocation != null)
            {
                zoneIDs.Add(conditionLeaveItemAtLocation.zoneId);
            }

            ConditionPlaceBeacon conditionPlaceBeacon = condition as ConditionPlaceBeacon;
            if (conditionPlaceBeacon != null)
            {
                zoneIDs.Add(conditionPlaceBeacon.zoneId);
            }

            ConditionLaunchFlare conditionLaunchFlare = condition as ConditionLaunchFlare;
            if (conditionLaunchFlare != null)
            {
                zoneIDs.Add(conditionLaunchFlare.zoneID);
            }

            ConditionVisitPlace conditionVisitPlace = condition as ConditionVisitPlace;
            if (conditionVisitPlace != null)
            {
                zoneIDs.Add(conditionVisitPlace.target);
            }

            ConditionInZone conditionInZone = condition as ConditionInZone;
            if (conditionInZone != null)
            {
                zoneIDs.AddRange(conditionInZone.zoneIds);
            }

            ConditionCounterCreator conditionCounterCreator = condition as ConditionCounterCreator;
            if (conditionCounterCreator != null)
            {
                foreach (Condition childCondition in conditionCounterCreator.counter.conditions)
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(childCondition));
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                zoneIDs.AddRange(getAllZoneIDsForQuestCondition(childCondition));
            }

            return zoneIDs.Distinct();
        }

        private void ProcessTrigger(TriggerWithId trigger)
        {
            // Skip zones that have already been processed
            if (zoneIDsInLocation.Contains(trigger.Id))
            {
                return;
            }

            // Find all quests that have objectives using this trigger
            Quest[] matchingQuests = allQuests.Where(q => q.GetObjectiveForZoneID(trigger.Id) != null).ToArray();
            if (matchingQuests.Length == 0)
            {
                //LoggingController.LogInfo("No matching quests for trigger " + trigger.Id);
                return;
            }

            // Ensure there is a collider for the trigger
            Collider triggerCollider = trigger.gameObject.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                LoggingController.LogError("Trigger " + trigger.Id + " has no collider");
                return;
            }

            // Set the target location to be in the center of the collider. If the collider is very large (i.e. for an entire building), set the
            // target location to be just above the floor. 
            Vector3 triggerTargetPosition = triggerCollider.bounds.center;
            if (triggerCollider.bounds.extents.y > 1.5f)
            {
                triggerTargetPosition.y = triggerCollider.bounds.min.y + 0.75f;
                LoggingController.LogInfo("Adjusting position for zone " + trigger.Id + " to " + triggerTargetPosition.ToString());
            }

            // Determine how far to search for a valid NavMesh position from the target location. If the collider (zone) is very large, expand the search range.
            // TO DO: This is kinda sloppy and should be fixed. 
            float maxSearchDistance = ConfigController.Config.QuestGeneration.NavMeshSearchDistanceZone;
            maxSearchDistance *= triggerCollider.bounds.Volume() > 20 ? 2 : 1;
            Vector3? navMeshTargetPoint = LocationController.FindNearestNavMeshPosition(triggerTargetPosition, maxSearchDistance);
            if (!navMeshTargetPoint.HasValue)
            {
                LoggingController.LogError("Cannot find NavMesh point for trigger " + trigger.Id);
                return;
            }

            // Add a step with the NavMesh position to corresponding objectives in every quest using this zone
            foreach (Quest quest in matchingQuests)
            {
                LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest.Name);

                QuestObjective objective = quest.GetObjectiveForZoneID(trigger.Id);
                objective.AddStep(new QuestObjectiveStep(navMeshTargetPoint.Value));

                // If the zone is large, allow twice as many bots to do the objective at the same time
                // TO DO: This is kinda sloppy and should be fixed. 
                int maxBots = ConfigController.Config.BotQuests.EFTQuests.MaxBotsPerQuest;
                objective.MaxBots *= triggerCollider.bounds.Volume() > 5 ? maxBots * 2 : maxBots;

                zoneIDsInLocation.Add(trigger.Id);
            }

            if (ConfigController.Config.Debug.ShowZoneOutlines)
            {
                Vector3[] triggerColliderBounds = PathRender.GetBoundingBoxPoints(triggerCollider.bounds);
                PathVisualizationData triggerBoundingBox = new PathVisualizationData("Trigger_" + trigger.Id, triggerColliderBounds, Color.cyan);
                PathRender.AddOrUpdatePath(triggerBoundingBox);

                Vector3[] triggerTargetPoint = PathRender.GetSpherePoints(navMeshTargetPoint.Value, 0.5f, 10);
                PathVisualizationData triggerTargetPosSphere = new PathVisualizationData("TriggerTargetPos_" + trigger.Id, triggerTargetPoint, Color.cyan);
                PathRender.AddOrUpdatePath(triggerTargetPosSphere);
            }
        }

        private void WriteQuestLogFile()
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
                + previousLocationID.Replace(" ", "")
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

        private string GetBotListText(IEnumerable<BotOwner> bots)
        {
            return string.Join(",", bots.Select(b => b.Profile.Nickname + " (Level " + b.Profile.Info.Level + ")"));
        }
    }
}
