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
using SPTQuestingBots.Models.Questing;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class QuestHelpers
    {
        private static Dictionary<string, Configuration.ZoneAndItemPositionInfoConfig> zoneAndItemQuestPositions = null;
        private static Dictionary<Condition, IEnumerable<string>> allZoneIDsForCondition = new Dictionary<Condition, IEnumerable<string>>();
        private static Dictionary<Condition, float?> plantTimeForCondition = new Dictionary<Condition, float?>();
        private static Dictionary<Condition, float?> beaconTimeForCondition = new Dictionary<Condition, float?>();
        
        public static void ClearCache()
        {
            allZoneIDsForCondition.Clear();
            plantTimeForCondition.Clear();
            beaconTimeForCondition.Clear();

            QuestMinLevelFinder.ClearCache();
        }

        public static bool ValidateQuestFiles(string locationId)
        {
            IEnumerable<Quest> quests = ConfigController.GetCustomQuests(locationId);

            if (!quests.Any())
            {
                LoggingController.LogWarningToServerConsole("Could not find any non-EFT quests for " + locationId);
                return false;
            }

            LoggingController.LogInfo("Found " + quests.Count() + " non-EFT quests for " + locationId);
            return true;
        }

        public static IReadOnlyDictionary<string, Configuration.ZoneAndItemPositionInfoConfig> LoadZoneAndItemQuestPositions()
        {
            if (zoneAndItemQuestPositions != null)
            {
                return zoneAndItemQuestPositions;
            }

            zoneAndItemQuestPositions = ConfigController.GetZoneAndItemPositions();
            LoggingController.LogInfo("Found override settings for " + zoneAndItemQuestPositions.Count + " zone or item position(s)");

            return zoneAndItemQuestPositions;
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
            float? plantTime = quest.findQuestValue(zoneID, FindPlantTime);
            if (plantTime.HasValue)
            {
                return plantTime.Value;
            }

            return null;
        }

        public static float? FindBeaconTime(this Quest quest, string zoneID)
        {
            float? beaconTime = quest.findQuestValue(zoneID, FindBeaconTime);
            if (beaconTime.HasValue)
            {
                return beaconTime.Value;
            }

            return null;
        }

        public static void LocateQuestItems(this Quest quest, IEnumerable<LootItem> allLoot)
        {
            LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();
            if (locationData == null)
            {
                throw new InvalidOperationException("Cannot access location data");
            }

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

                    Vector3 itemPosition = itemCollider.bounds.center;
                    string doorIDToUnlock = "";
                    Models.SerializableVector3 interactionPositionForDoorToUnlock = null;
                    if (zoneAndItemQuestPositions.ContainsKey(target))
                    {
                        // Check if a specific position should be used for bots to get the item
                        if (zoneAndItemQuestPositions[target].Position != null)
                        {
                            itemPosition = zoneAndItemQuestPositions[target].Position.ToUnityVector3();
                            LoggingController.LogInfo("Using override position for " + item.Item.LocalizedName());
                        }

                        // Check if bots should open a specific door to get the item
                        if (zoneAndItemQuestPositions[target].MustUnlockNearbyDoor)
                        {
                            IEnumerable<WorldInteractiveObject> matchingWorldInteractiveObjects = locationData.FindAllWorldInteractiveObjectsNearPosition(itemPosition, zoneAndItemQuestPositions[target].NearbyDoorSearchRadius);

                            int matchingDoorCount = matchingWorldInteractiveObjects.Count();
                            if (matchingDoorCount == 0)
                            {
                                LoggingController.LogInfo("Cannot find any doors to unlock for item " + item.Item.LocalizedName() + " for quest " + quest.Name);
                            }
                            if (matchingDoorCount > 1)
                            {
                                LoggingController.LogInfo("Found too many doors to unlock for item " + item.Item.LocalizedName() + " for quest " + quest.Name);
                            }
                            if (matchingDoorCount == 1)
                            {
                                doorIDToUnlock = matchingWorldInteractiveObjects.First().Id;
                                interactionPositionForDoorToUnlock = zoneAndItemQuestPositions[target].NearbyDoorInteractionPosition;
                                LoggingController.LogDebug("WorldInteractiveObject " + doorIDToUnlock + " must be unlocked for item " + item.Item.LocalizedName() + " for quest " + quest.Name);
                            }
                        }
                    }

                    // Try to find the nearest NavMesh position next to the quest item.
                    Vector3? navMeshTargetPoint = locationData.FindNearestNavMeshPosition(itemPosition, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceItem);
                    if (!navMeshTargetPoint.HasValue)
                    {
                        LoggingController.LogError("Cannot find NavMesh point for quest item " + item.Item.LocalizedName());

                        if (ConfigController.Config.Debug.ShowZoneOutlines)
                        {
                            Vector3[] itemPositionOutline = DebugHelpers.GetSpherePoints(item.transform.position, 0.5f, 10);
                            Models.Pathing.PathVisualizationData itemPositionSphere = new Models.Pathing.PathVisualizationData("QuestItem_" + item.Item.LocalizedName(), itemPositionOutline, Color.red);
                            Singleton<GameWorld>.Instance.GetComponent<PathRenderer>().AddOrUpdatePath(itemPositionSphere);
                        }

                        continue;
                    }

                    // Add an objective for the quest item using the nearest valid NavMesh position to it
                    QuestObjective newObjective = new QuestItemObjective(item, navMeshTargetPoint.Value);
                    newObjective.DoorIDToUnlock = doorIDToUnlock;
                    newObjective.InteractionPositionToUnlockDoor = interactionPositionForDoorToUnlock;
                    quest.AddObjective(newObjective);

                    LoggingController.LogDebug("Found " + item.Item.LocalizedName() + " for quest " + quest.Name);
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

            float? plantTime = condition.recursiveConditionSearch(zoneID, FindPlantTime);
            if (plantTime.HasValue)
            {
                plantTimeForCondition.Add(condition, plantTime.Value);
                return plantTime.Value;
            }

            plantTimeForCondition.Add(condition, null);
            return null;
        }

        private static float? FindBeaconTime(this Condition condition, string zoneID)
        {
            if (beaconTimeForCondition.ContainsKey(condition))
            {
                return beaconTimeForCondition[condition];
            }

            ConditionPlaceBeacon conditionLeaveItemAtLocation = condition as ConditionPlaceBeacon;
            if (conditionLeaveItemAtLocation?.zoneId == zoneID)
            {
                if (conditionLeaveItemAtLocation.plantTime > 0)
                {
                    beaconTimeForCondition.Add(condition, conditionLeaveItemAtLocation.plantTime);
                    return conditionLeaveItemAtLocation.plantTime;
                }
            }

            float? plantTime = condition.recursiveConditionSearch(zoneID, FindBeaconTime);
            if (plantTime.HasValue)
            {
                beaconTimeForCondition.Add(condition, plantTime.Value);
                return plantTime.Value;
            }

            beaconTimeForCondition.Add(condition, null);
            return null;
        }

        private static T findQuestValue<T>(this Quest quest, string zoneID, Func<Condition, string, T> searchMethod)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    T value = searchMethod(condition, zoneID);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }

            return default(T);
        }

        private static T recursiveConditionSearch<T>(this Condition condition, string zoneID, Func<Condition, string, T> searchMethod)
        {
            ConditionCounterCreator conditionCounterCreator = condition as ConditionCounterCreator;
            if (conditionCounterCreator != null)
            {
                foreach (Condition childCondition in conditionCounterCreator.Conditions)
                {
                    T value = searchMethod(childCondition, zoneID);
                    if (value != null)
                    {
                        return value;
                    }
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                T value = searchMethod(childCondition, zoneID);
                if (value != null)
                {
                    return value;
                }
            }

            return default(T);
        }
    }
}
