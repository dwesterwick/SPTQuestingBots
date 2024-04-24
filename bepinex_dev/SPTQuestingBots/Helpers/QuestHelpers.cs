using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT;
using EFT.Quests;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class QuestHelpers
    {
        private static Dictionary<string, int> minLevelForQuest = new Dictionary<string, int>();
        private static Dictionary<Condition, IEnumerable<string>> allZoneIDsForCondition = new Dictionary<Condition, IEnumerable<string>>();
        private static Dictionary<Condition, float?> plantTimeForCondition = new Dictionary<Condition, float?>();

        public static void ClearCache()
        {
            minLevelForQuest.Clear();
            allZoneIDsForCondition.Clear();
            plantTimeForCondition.Clear();
        }

        public static int GetMinLevel(this Quest quest)
        {
            if (minLevelForQuest.ContainsKey(quest.Template?.Id))
            {
                return minLevelForQuest[quest.Template.Id];
            }

            // Be default, use the minimum level set for the quest template
            int minLevel = quest.Template?.Level ?? 0;

            EQuestStatus eQuestStatus = EQuestStatus.AvailableForStart;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
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
                        Quest preReqQuest = BotJobAssignmentFactory.FindQuest(preReqQuestID);
                        if (preReqQuest == null)
                        {
                            LoggingController.LogWarning("Cannot find prerequisite quest " + preReqQuestID + " for quest " + quest.Name);
                            continue;
                        }

                        // Get the minimum player level to start that quest
                        int minLevelForPreReqQuest = preReqQuest.GetMinLevel();
                        if (minLevelForPreReqQuest <= minLevel)
                        {
                            continue;
                        }

                        minLevel = minLevelForPreReqQuest;
                    }
                }
            }

            if (quest.Template != null)
            {
                minLevelForQuest.Add(quest.Template.Id, minLevel);
            }

            return minLevel;
        }

        public static IEnumerable<string> GetAllZoneIDs(this Quest quest)
        {
            List<string> zoneIDs = new List<string>();
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    zoneIDs.AddRange(condition.getAllZoneIDs());
                }
            }

            return zoneIDs;
        }

        public static float? FindPlantTime(this Quest quest, string zoneID)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    float? plantTime = condition.FindPlantTime(zoneID);
                    if (plantTime.HasValue)
                    {
                        return plantTime.Value;
                    }
                }
            }

            return null;
        }

        public static void LocateQuestItems(this Quest quest, IEnumerable<LootItem> allLoot)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
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
                    Vector3? navMeshTargetPoint = Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindNearestNavMeshPosition(itemCollider.bounds.center, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceItem);
                    if (!navMeshTargetPoint.HasValue)
                    {
                        LoggingController.LogError("Cannot find NavMesh point for quest item " + item.Item.LocalizedName());

                        if (ConfigController.Config.Debug.ShowZoneOutlines)
                        {
                            Vector3[] itemPositionOutline = DebugHelpers.GetSpherePoints(item.transform.position, 0.5f, 10);
                            PathVisualizationData itemPositionSphere = new PathVisualizationData("QuestItem_" + item.Item.LocalizedName(), itemPositionOutline, Color.red);
                            Singleton<GameWorld>.Instance.GetComponent<PathRender>().AddOrUpdatePath(itemPositionSphere);
                        }

                        continue;
                    }

                    // Add an objective for the quest item using the nearest valid NavMesh position to it
                    quest.AddObjective(new QuestItemObjective(item, navMeshTargetPoint.Value));
                    LoggingController.LogInfo("Found " + item.Item.LocalizedName() + " for quest " + quest.Name);
                }
            }
        }

        private static IEnumerable<string> getAllZoneIDs(this Condition condition)
        {
            if (allZoneIDsForCondition.ContainsKey(condition))
            {
                return allZoneIDsForCondition[condition];
            }

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
                foreach (Condition childCondition in conditionCounterCreator.Conditions)
                {
                    zoneIDs.AddRange(childCondition.getAllZoneIDs());
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                zoneIDs.AddRange(childCondition.getAllZoneIDs());
            }

            IEnumerable<string> zoneIDsDistinct = zoneIDs.Distinct();

            allZoneIDsForCondition.Add(condition, zoneIDsDistinct);

            return zoneIDsDistinct;
        }

        private static float? FindPlantTime(this Condition condition, string zoneID)
        {
            if (plantTimeForCondition.ContainsKey(condition))
            {
                return plantTimeForCondition[condition];
            }

            ConditionLeaveItemAtLocation conditionLeaveItemAtLocation = condition as ConditionLeaveItemAtLocation;
            if (conditionLeaveItemAtLocation?.zoneId == zoneID)
            {
                if (conditionLeaveItemAtLocation.plantTime > 0)
                {
                    plantTimeForCondition.Add(condition, conditionLeaveItemAtLocation.plantTime);
                    return conditionLeaveItemAtLocation.plantTime;
                }
            }

            ConditionCounterCreator conditionCounterCreator = condition as ConditionCounterCreator;
            if (conditionCounterCreator != null)
            {
                foreach (Condition childCondition in conditionCounterCreator.Conditions)
                {
                    float? plantTime = childCondition.FindPlantTime(zoneID);
                    if (plantTime.HasValue)
                    {
                        plantTimeForCondition.Add(condition, plantTime.Value);
                        return plantTime.Value;
                    }
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                float? plantTime = childCondition.FindPlantTime(zoneID);
                if (plantTime.HasValue)
                {
                    plantTimeForCondition.Add(condition, plantTime.Value);
                    return plantTime.Value;
                }
            }

            plantTimeForCondition.Add(condition, null);
            return null;
        }
    }
}
