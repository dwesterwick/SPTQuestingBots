using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.Quests;
using QuestingBots.BotLogic;
using QuestingBots.Models;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Controllers
{
    public class BotQuestController : MonoBehaviour
    {
        public static bool IsFindingTriggers = false;
        public static bool HaveTriggersBeenFound = false;

        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static List<Quest> allQuests = new List<Quest>();
        private static List<string> zoneIDsInLocation = new List<string>();
        private static List<BotOwner> pmcsInLocation = new List<BotOwner>();
        private static List<BotOwner> bossesInLocation = new List<BotOwner>();
        private static Dictionary<BotOwner, List<BotOwner>> bossFollowersInLocation = new Dictionary<BotOwner, List<BotOwner>>();

        public void Clear()
        {
            if (IsFindingTriggers)
            {
                enumeratorWithTimeLimit.Abort();
                CoroutineExtensions.TaskWithTimeLimit.WaitForCondition(() => !IsFindingTriggers);
            }

            allQuests.RemoveAll(q => q.Template == null);

            foreach (Quest quest in allQuests)
            {
                quest.Clear();
            }

            pmcsInLocation.Clear();
            bossesInLocation.Clear();
            bossFollowersInLocation.Clear();
            zoneIDsInLocation.Clear();

            HaveTriggersBeenFound = false;
        }

        private void Update()
        {
            if (LocationController.CurrentLocation == null)
            {
                Clear();
                return;
            }

            if (IsFindingTriggers || HaveTriggersBeenFound)
            {
                return;
            }

            StartCoroutine(LoadAllQuests());
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
            if (!bossFollowersInLocation.ContainsKey(botOwner))
            {
                return new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
            }

            return new ReadOnlyCollection<BotOwner>(bossFollowersInLocation[botOwner].Where(b => (b.BotState == EBotState.Active) && !b.IsDead).ToArray());
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
            List<Quest> applicableQuests = allQuests
                .Where(q => q.CanAssignBot(bot))
                .Where(q => q.NumberOfValidObjectives > 0)
                .ToList();
            
            if (applicableQuests.Count == 0)
            {
                return null;
            }

            System.Random random = new System.Random();
            IEnumerable<Quest> prioritizedQuests = applicableQuests.OrderBy(q => q.Priority * 100 + random.NextFloat(-1, 1));
            //LoggingController.LogInfo("Possible quests (in order of priority) for bot " + bot.Profile.Nickname + ": " + string.Join(", ", orderedQuests.Select(q => q.Name)));

            foreach (Quest quest in prioritizedQuests)
            {
                if (random.NextFloat(0, 100) < quest.ChanceForSelecting)
                {
                    return quest;
                }
            }

            return prioritizedQuests.First();
        }

        private IEnumerator LoadAllQuests()
        {
            IsFindingTriggers = true;

            try
            {
                if (allQuests.Count == 0)
                {
                    RawQuestClass[] allQuestTemplates = ConfigController.GetAllQuestTemplates();
                    foreach (RawQuestClass questTemplate in allQuestTemplates)
                    {
                        Quest quest = new Quest(ConfigController.Config.BotQuests.EFTQuests.Priority, questTemplate);
                        quest.ChanceForSelecting = ConfigController.Config.BotQuests.EFTQuests.Chance;
                        allQuests.Add(quest);
                    }
                }

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allQuests, LoadQuest);

                IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();
                //IEnumerable<Type> allTriggerTypes = allTriggers.Select(t => t.GetType()).Distinct();
                //LoggingController.LogInfo("Found " + allTriggers.Count() + " triggers of types: " + string.Join(", ", allTriggerTypes));

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allTriggers, ProcessTrigger);

                //IEnumerable<LootItem> allLoot = FindObjectsOfType<LootItem>(); <-- this does not work for inactive quest items!
                IEnumerable<LootItem> allItems = Singleton<GameWorld>.Instance.LootItems.Where(i => i.Item != null).Distinct(i => i.TemplateId);

                //IEnumerable<LootItem> allQuestItems = allItems.Where(l => l.Item.QuestItem);
                //LoggingController.LogInfo("Quest items: " + string.Join(", ", allQuestItems.Select(l => l.Item.LocalizedName())));

                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(allQuests, LocateQuestItems, allItems);

                Quest spawnPointQuest = LocationController.CreateSpawnPointQuest();
                if (spawnPointQuest != null)
                {
                    allQuests.Add(spawnPointQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for going to random spawn points");
                }

                if (LocationController.GetElapsedRaidTime() < ConfigController.Config.BotQuests.SpawnRush.MaxRaidET)
                {
                    Quest spawnRushQuest = LocationController.CreateSpawnRushQuest();
                    if (spawnRushQuest != null)
                    {
                        allQuests.Add(spawnRushQuest);
                    }
                    else
                    {
                        LoggingController.LogError("Could not add quest for rushing your spawn point");
                    }
                }
                else
                {
                    LoggingController.LogInfo("Too much time has elapsed in the raid to add a spawn-rush quest.");
                }

                HaveTriggersBeenFound = true;
                LoggingController.LogInfo("Finished loading quest data.");
            }
            finally
            {
                IsFindingTriggers = false;
            }
        }

        private void LocateQuestItems(Quest quest, IEnumerable<LootItem> allLoot)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
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

                    QuestObjective objective = quest.GetObjectiveForLootItem(target);
                    if (objective != null)
                    {
                        continue;
                    }

                    IEnumerable<LootItem> matchingLootItems = allLoot.Where(l => l.TemplateId == target);
                    if (matchingLootItems.Count() == 0)
                    {
                        continue;
                    }

                    LootItem item = matchingLootItems.First();
                    if (item.Item.QuestItem)
                    {
                        Collider itemCollider = item.GetComponent<Collider>();
                        if (itemCollider == null)
                        {
                            LoggingController.LogError("Quest item " + item.Item.LocalizedName() + " has no collider");
                            return;
                        }

                        Vector3? navMeshTargetPoint = LocationController.FindNearestNavMeshPosition(itemCollider.bounds.center, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceItem);
                        if (!navMeshTargetPoint.HasValue)
                        {
                            LoggingController.LogError("Cannot find NavMesh point for quest item " + item.Item.LocalizedName());
                            return;
                        }

                        quest.AddObjective(new QuestItemObjective(item, navMeshTargetPoint.Value));
                        LoggingController.LogInfo("Found " + item.Item.LocalizedName() + " for quest " + quest.Name);
                    }
                }
            }
        }

        private void LoadQuest(Quest quest)
        {
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
                if (quest.GetObjectiveForZoneID(zoneID) != null)
                {
                    continue;
                }

                QuestZoneObjective objective = new QuestZoneObjective(zoneID);
                objective.MaxBots = ConfigController.Config.BotQuests.EFTQuests.MaxBotsPerQuest;
                quest.AddObjective(objective);
            }

            int minLevel = getMinLevelForQuest(quest);
            //LoggingController.LogInfo("Min level for quest \"" + quest.Name + "\": " + minLevel);
            quest.MinLevel = minLevel;
        }

        private int getMinLevelForQuest(Quest quest)
        {
            int minLevel = quest.Template.Level;
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForStart;
            if (quest.Template.Conditions.ContainsKey(eQuestStatus))
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    ConditionLevel conditionLevel = condition as ConditionLevel;
                    if (conditionLevel != null)
                    {
                        if ((conditionLevel.compareMethod == ECompareMethod.MoreOrEqual) || (conditionLevel.compareMethod == ECompareMethod.More))
                        {
                            if (conditionLevel.value > minLevel)
                            {
                                minLevel = (int)conditionLevel.value;
                            }
                        }
                    }

                    ConditionQuest conditionQuest = condition as ConditionQuest;
                    if (conditionQuest != null)
                    {
                        string preReqQuestID = conditionQuest.target;
                        Quest preReqQuest = allQuests.First(q => q.Template.Id == preReqQuestID);

                        int minLevelForPreReqQuest = getMinLevelForQuest(preReqQuest);
                        if (minLevelForPreReqQuest > minLevel)
                        {
                            minLevel = minLevelForPreReqQuest;
                        }
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
            Quest[] matchingQuests = allQuests.Where(q => q.GetObjectiveForZoneID(trigger.Id) != null).ToArray();
            if (matchingQuests.Length == 0)
            {
                //LoggingController.LogInfo("No matching quests for trigger " + trigger.Id);
                return;
            }

            Collider triggerCollider = trigger.gameObject.GetComponent<Collider>();
            if (triggerCollider == null)
            {
                LoggingController.LogError("Trigger " + trigger.Id + " has no collider");
                return;
            }

            Vector3 triggerTargetPosition = triggerCollider.bounds.center;
            if (triggerCollider.bounds.extents.y > 1.5f)
            {
                triggerTargetPosition.y = triggerCollider.bounds.min.y + 0.75f;
                LoggingController.LogInfo("Adjusting position for zone " + trigger.Id + " to " + triggerTargetPosition.ToString());
            }

            float maxSearchDistance = ConfigController.Config.QuestGeneration.NavMeshSearchDistanceZone;
            maxSearchDistance *= triggerCollider.bounds.Volume() > 20 ? 2 : 1;
            Vector3? navMeshTargetPoint = LocationController.FindNearestNavMeshPosition(triggerTargetPosition, maxSearchDistance);
            if (!navMeshTargetPoint.HasValue)
            {
                LoggingController.LogError("Cannot find NavMesh point for trigger " + trigger.Id);
                return;
            }

            foreach (Quest quest in matchingQuests)
            {
                if (zoneIDsInLocation.Contains(trigger.Id))
                {
                    continue;
                }

                LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest.Name);

                QuestObjective objective = quest.GetObjectiveForZoneID(trigger.Id);
                objective.Position = navMeshTargetPoint.Value;
                objective.MaxBots *= triggerCollider.bounds.Volume() > 5 ? 2 : 1;

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
    }
}
