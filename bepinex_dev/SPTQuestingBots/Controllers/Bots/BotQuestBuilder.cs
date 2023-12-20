using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using EFT.Quests;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Controllers.Bots
{
    public class BotQuestBuilder : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool IsBuildingQuests { get; private set; } = false;
        public static bool HaveQuestsBeenBuilt { get; private set; } = false;
        public static string PreviousLocationID { get; private set; } = null;

        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static List<string> zoneIDsInLocation = new List<string>();
        
        public IEnumerator Clear()
        {
            IsClearing = true;

            if (IsBuildingQuests)
            {
                enumeratorWithTimeLimit.Abort();

                CoroutineExtensions.EnumeratorWithTimeLimit conditionWaiter = new CoroutineExtensions.EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsBuildingQuests, nameof(IsBuildingQuests), 3000);

                IsBuildingQuests = false;
            }

            HaveQuestsBeenBuilt = false;
            zoneIDsInLocation.Clear();

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
                if (IsBuildingQuests || HaveQuestsBeenBuilt)
                {
                    LoggingController.LogInfo("Clearing quest data...");
                }

                StartCoroutine(Clear());
                return;
            }

            if (IsBuildingQuests || HaveQuestsBeenBuilt)
            {
                return;
            }

            LocationController.FindAllInteractiveObjects();
            StartCoroutine(LoadAllQuests());

            // Store the name of the current location so it can be used when writing the quest log file. The current location will be null when the log is written.
            PreviousLocationID = LocationController.CurrentLocation.Id;
        }

        private IEnumerator LoadAllQuests()
        {
            IsBuildingQuests = true;

            try
            {
                if (BotJobAssignmentFactory.QuestCount == 0)
                {
                    // Create quests based on the EFT quest templates loaded from the server. This may include custom quests added by mods. 
                    RawQuestClass[] allQuestTemplates = ConfigController.GetAllQuestTemplates();

                    foreach (RawQuestClass questTemplate in allQuestTemplates)
                    {
                        Quest quest = new Quest(ConfigController.Config.Questing.BotQuests.EFTQuests.Priority, questTemplate);
                        quest.ChanceForSelecting = ConfigController.Config.Questing.BotQuests.EFTQuests.Chance;
                        quest.PMCsOnly = true;
                        BotJobAssignmentFactory.AddQuest(quest);
                    }
                }

                // Process each of the quests created by an EFT quest template using the provided action
                yield return BotJobAssignmentFactory.ProcessAllQuests(LoadQuest);

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
                yield return BotJobAssignmentFactory.ProcessAllQuests(LocateQuestItems, allItems);

                // Update the chance that bots will have keys for EFT quests
                yield return BotJobAssignmentFactory.ProcessAllQuests(updateChancesOfBotsHavingKeys, ConfigController.Config.Questing.BotQuests.EFTQuests.ChanceOfHavingKeys);

                // Create a quest where the bots wanders to various spawn points around the map. This was implemented as a stop-gap for maps with few other quests.
                Quest spawnPointQuest = createSpawnPointQuest();
                if (spawnPointQuest != null)
                {
                    BotJobAssignmentFactory.AddQuest(spawnPointQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for going to random spawn points");
                }

                // Create a quest where initial PMC's can run to your spawn point (not directly to you). 
                Quest spawnRushQuest = createSpawnRushQuest();
                if (spawnRushQuest != null)
                {
                    BotJobAssignmentFactory.AddQuest(spawnRushQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for rushing your spawn point");
                }

                // Create a quest where initial PMC's can run to your spawn point (not directly to you). 
                Quest bossHunterQuest = createBossHunterQuest();
                if (bossHunterQuest != null)
                {
                    BotJobAssignmentFactory.AddQuest(bossHunterQuest);
                }
                else
                {
                    LoggingController.LogWarning("Could not add quest for hunting bosses. This is normal if bosses do not spawn on this map.");
                }

                LoadCustomQuests();

                BotJobAssignmentFactory.RemoveBlacklistedQuestObjectives(LocationController.CurrentLocation.Id);

                HaveQuestsBeenBuilt = true;
                LoggingController.LogInfo("Finished loading quest data.");
            }
            finally
            {
                IsBuildingQuests = false;
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
                    bool removeObjective = false;

                    if (!removeObjective && !objective.TryFindAllInteractiveObjects())
                    {
                        removeObjective = true;
                        LoggingController.LogError("Could not find required interactive objects for all steps in objective " + objective.ToString() + " for quest " + quest.Name);
                    }

                    if (!removeObjective && !objective.TrySnapAllStepPositionsToNavMesh())
                    {
                        removeObjective = true;
                        LoggingController.LogError("Could not find valid NavMesh positions for all steps in objective " + objective.ToString() + " for quest " + quest.Name);
                    }

                    if (removeObjective && !quest.TryRemoveObjective(objective))
                    {
                        LoggingController.LogError("Could not remove objective " + objective.ToString());
                        objective.DeleteAllSteps();
                    }
                }

                // Do not use quests that don't have any valid objectives (using the check above)
                if (!quest.ValidObjectives.Any() || quest.ValidObjectives.All(o => o.StepCount == 0))
                {
                    LoggingController.LogError("Could not find any valid objectives for quest " + quest.Name + ". Disabling quest.");
                    continue;
                }

                BotJobAssignmentFactory.AddQuest(quest);
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
                    Vector3? navMeshTargetPoint = LocationController.FindNearestNavMeshPosition(itemCollider.bounds.center, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceItem);
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
            quest.MaxBots = ConfigController.Config.Questing.BotQuests.EFTQuests.MaxBotsPerQuest;

            // Enumerate all zones used by the quest (in any of its objectives)
            IEnumerable<string> zoneIDs = getAllZoneIDsForQuest(quest);

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
                quest.AddObjective(objective);
            }

            // Calculate the minimum and maximum player levels allowed for selecting the quest
            quest.MinLevel = getMinLevelForQuest(quest);
            double levelRange = ConfigController.InterpolateForFirstCol(ConfigController.Config.Questing.BotQuests.EFTQuests.LevelRange, quest.MinLevel);
            quest.MaxLevel = quest.MinLevel + (int)Math.Ceiling(levelRange);

            //LoggingController.LogInfo("Level range for quest \"" + quest.Name + "\": " + quest.MinLevel + "-" + quest.MaxLevel);
        }

        private int getMinLevelForQuest(Quest quest)
        {
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

        private IEnumerable<string> getAllZoneIDsForQuest(Quest quest)
        {
            List<string> zoneIDs = new List<string>();
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    zoneIDs.AddRange(getAllZoneIDsForQuestCondition(condition));
                }
            }

            return zoneIDs;
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

        private float? findPlantTimeForQuest(Quest quest, string zoneID)
        {
            EQuestStatus eQuestStatus = EQuestStatus.AvailableForFinish;
            if (quest.Template?.Conditions?.ContainsKey(eQuestStatus) == true)
            {
                foreach (Condition condition in quest.Template.Conditions[eQuestStatus])
                {
                    float? plantTime = findPlantTimeForQuestCondition(condition, zoneID);
                    if (plantTime.HasValue)
                    {
                        return plantTime.Value;
                    }
                }
            }

            return null;
        }

        private float? findPlantTimeForQuestCondition(Condition condition, string zoneID)
        {
            ConditionLeaveItemAtLocation conditionLeaveItemAtLocation = condition as ConditionLeaveItemAtLocation;
            if (conditionLeaveItemAtLocation?.zoneId == zoneID)
            {
                if (conditionLeaveItemAtLocation.plantTime > 0)
                {
                    return conditionLeaveItemAtLocation.plantTime;
                }
            }

            ConditionCounterCreator conditionCounterCreator = condition as ConditionCounterCreator;
            if (conditionCounterCreator != null)
            {
                foreach (Condition childCondition in conditionCounterCreator.counter.conditions)
                {
                    float? plantTime = findPlantTimeForQuestCondition(childCondition, zoneID);
                    if (plantTime.HasValue)
                    {
                        return plantTime.Value;
                    }
                }
            }

            foreach (Condition childCondition in condition.ChildConditions)
            {
                float? plantTime = findPlantTimeForQuestCondition(childCondition, zoneID);
                if (plantTime.HasValue)
                {
                    return plantTime.Value;
                }
            }

            return null;
        }

        private void ProcessTrigger(TriggerWithId trigger)
        {
            // Skip zones that have already been processed
            if (zoneIDsInLocation.Contains(trigger.Id))
            {
                return;
            }

            // Find all quests that have objectives using this trigger
            Quest[] matchingQuests = BotJobAssignmentFactory.FindQuestsWithZone(trigger.Id);
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
            float maxSearchDistance = ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceZone;
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

                float? plantTime = findPlantTimeForQuest(quest, trigger.Id);
                if (plantTime.HasValue)
                {
                    LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest.Name + " - Adding plant time: " + plantTime.Value + "s");

                    objective.AddStep(new QuestObjectiveStep(navMeshTargetPoint.Value, QuestAction.PlantItem, plantTime.Value));
                }

                // If the zone is large, allow twice as many bots to do the objective at the same time
                // TO DO: This is kinda sloppy and should be fixed. 
                int maxBots = ConfigController.Config.Questing.BotQuests.EFTQuests.MaxBotsPerQuest;
                quest.MaxBots *= triggerCollider.bounds.Volume() > 5 ? maxBots * 2 : maxBots;

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

        private static void updateChancesOfBotsHavingKeys(Models.Quest quest, float chance)
        {
            foreach (QuestObjective objective in quest.AllObjectives)
            {
                foreach (QuestObjectiveStep step in objective.AllSteps)
                {
                    step.ChanceOfHavingKey = chance;
                }
            }
        }

        

        private static Models.Quest createSpawnPointQuest(ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All)
        {
            // Ensure the map has spawn points
            IEnumerable<SpawnPointParams> eligibleSpawnPoints = LocationController.CurrentLocation.SpawnPointParams.Where(s => s.Categories.Any(spawnTypes));
            if (eligibleSpawnPoints.IsNullOrEmpty())
            {
                return null;
            }

            Models.Quest quest = new Models.Quest(ConfigController.Config.Questing.BotQuests.SpawnPointWander.Priority, "Spawn Points");
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, ConfigController.Config.Questing.BotQuests.SpawnPointWander);

            foreach (SpawnPointParams spawnPoint in eligibleSpawnPoints)
            {
                // Ensure the spawn point has a valid nearby NavMesh position
                Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(spawnPoint.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogWarning("Cannot find NavMesh position for spawn point " + spawnPoint.Position.ToUnityVector3().ToString());
                    continue;
                }

                Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(spawnPoint, spawnPoint.Position);
                QuestSettingsConfig.ApplyQuestSettingsFromConfig(objective, ConfigController.Config.Questing.BotQuests.SpawnPointWander);
                quest.AddObjective(objective);
            }

            return quest;
        }

        private static Models.Quest createSpawnRushQuest()
        {
            SpawnPointParams? playerSpawnPoint = LocationController.GetPlayerSpawnPoint();
            if (!playerSpawnPoint.HasValue)
            {
                LoggingController.LogWarning("Cannot find player spawn point.");
                return null;
            }

            // Ensure there is a valid NavMesh position near your spawn point
            Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(playerSpawnPoint.Value.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
            if (!navMeshPosition.HasValue)
            {
                LoggingController.LogWarning("Cannot find NavMesh position for player spawn point.");
                return null;
            }

            //Vector3? playerPosition = LocationController.GetPlayerPosition();
            //LoggingController.LogInfo("Creating spawn rush quest for " + playerSpawnPoint.Value.Id + " via " + navMeshPosition.Value.ToString() + " for player at " + playerPosition.Value.ToString() + "...");

            Models.Quest quest = new Models.Quest(ConfigController.Config.Questing.BotQuests.SpawnRush.Priority, "Spawn Rush");
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, ConfigController.Config.Questing.BotQuests.SpawnRush);
            quest.PMCsOnly = true;

            Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(playerSpawnPoint.Value, navMeshPosition.Value);
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(objective, ConfigController.Config.Questing.BotQuests.SpawnRush);
            quest.AddObjective(objective);

            return quest;
        }

        private static Models.Quest createBossHunterQuest()
        {
            // Get all zones in which bosses can spawn and ensure that at least one exists
            IEnumerable<string> bossZones = getBossSpawnZones();
            if (!bossZones.Any())
            {
                return null;
            }

            Models.Quest quest = new Models.Quest(ConfigController.Config.Questing.BotQuests.BossHunter.Priority, "Boss Hunter");
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, ConfigController.Config.Questing.BotQuests.BossHunter);

            foreach (SpawnPointParams spawnPoint in LocationController.CurrentLocation.SpawnPointParams)
            {
                if (!bossZones.Contains(spawnPoint.BotZoneName))
                {
                    continue;
                }

                // Ensure the spawn point has a valid nearby NavMesh position
                Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(spawnPoint.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogWarning("Cannot find NavMesh position for spawn point " + spawnPoint.Position.ToUnityVector3().ToString());
                    continue;
                }

                Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(spawnPoint, spawnPoint.Position);
                QuestSettingsConfig.ApplyQuestSettingsFromConfig(objective, ConfigController.Config.Questing.BotQuests.BossHunter);
                quest.AddObjective(objective);
            }

            return quest;
        }

        private static IEnumerable<string> getBossSpawnZones()
        {
            List<string> bossZones = new List<string>();
            foreach (BossLocationSpawn bossLocationSpawn in LocationController.CurrentLocation.BossLocationSpawn)
            {
                if (ConfigController.Config.Questing.BotQuests.BlacklistedBossHunterBosses.Contains(bossLocationSpawn.BossName))
                {
                    continue;
                }

                bossZones.AddRange(bossLocationSpawn.BossZone.Split(','));
            }

            return bossZones.Distinct();
        }
    }
}
