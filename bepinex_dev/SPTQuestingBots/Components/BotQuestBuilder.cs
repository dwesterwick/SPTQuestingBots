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
using HarmonyLib;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using UnityEngine;
using static RootMotion.FinalIK.IKSolver;

namespace SPTQuestingBots.Components
{
    public class BotQuestBuilder : MonoBehaviour
    {
        public bool IsBuildingQuests { get; private set; } = false;
        public bool HaveQuestsBeenBuilt { get; private set; } = false;

        private CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private QuestPathFinder questPathFinder = new QuestPathFinder();
        private List<string> zoneIDsInLocation = new List<string>();
        
        private void Awake()
        {
            Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindAllInteractiveObjects();
            StartCoroutine(LoadAllQuests());
        }

        private void Update()
        {
            
        }

        public IList<StaticPathData> GetStaticPaths(Vector3 target)
        {
            return questPathFinder.GetStaticPaths(target);
        }

        public void AddAirdropChaserQuest(Vector3 airdropPosition)
        {
            if (airdropPosition == null)
            {
                throw new ArgumentNullException(nameof(airdropPosition));
            }

            if (!Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                LoggingController.LogError("Airdrop chaser quest cannot be added when the raid is not in-progress");
                return;
            }

            Models.Quest airdopChaserQuest = createGoToPositionQuest(airdropPosition, "Airdrop Chaser", ConfigController.Config.Questing.BotQuests.AirdropChaser);
            if (airdopChaserQuest != null)
            {
                float raidET = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetElapsedRaidSeconds();
                
                airdopChaserQuest.MaxRaidET = raidET + ConfigController.Config.Questing.BotQuests.AirdropBotInterestTime;
                BotJobAssignmentFactory.AddQuest(airdopChaserQuest);

                LoggingController.LogInfo("Added quest for the most recent airdop");
            }
            else
            {
                LoggingController.LogError("Could not add quest for the most recent airdop");
            }
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

                    Dictionary<string, Dictionary<string, object>> eftQuestOverrideSettings = ConfigController.GetEFTQuestSettings();
                    LoggingController.LogInfo("Found override settings for " + eftQuestOverrideSettings.Count + " EFT quest(s)");

                    QuestHelpers.LoadZoneAndItemQuestPositions();

                    foreach (RawQuestClass questTemplate in allQuestTemplates)
                    {
                        Quest quest = new Quest(questTemplate);
                        
                        QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, ConfigController.Config.Questing.BotQuests.EFTQuests);
                        quest.PMCsOnly = true;
                        
                        if (eftQuestOverrideSettings.ContainsKey(questTemplate.Id))
                        {
                            LoggingController.LogInfo("Applying override settings for quest " + quest.Name + "...");
                            quest.UpdateJSONProperties(eftQuestOverrideSettings[questTemplate.Id]);
                        }

                        BotJobAssignmentFactory.AddQuest(quest);
                    }
                }

                // Check which quests are currently active for the player
                ISession session = FindObjectOfType<QuestingBotsPlugin>().GetComponent<TarkovData>().GetSession();
                QuestDataClass[] activeQuests = session.Profile.QuestsData
                    .Where(q => q.Status == EFT.Quests.EQuestStatus.Started || q.Status == EFT.Quests.EQuestStatus.AvailableForFinish || q.Status == EFT.Quests.EQuestStatus.Success)
                    .ToArray();

                //string activeQuestNames = string.Join(", ", activeQuests.Select(q => q.Template.Name + " (" + q.Template.Id + ")"));
                //LoggingController.LogInfo("There are " + activeQuests.Length + " active quests for the current player: " + activeQuestNames);

                // Process each of the quests created by an EFT quest template
                yield return BotJobAssignmentFactory.ProcessAllQuests(LoadQuest, activeQuests);

                // Create quest objectives for all matching trigger colliders found in the map
                enumeratorWithTimeLimit.Reset();
                IEnumerable<TriggerWithId> allTriggers = FindObjectsOfType<TriggerWithId>();
                yield return enumeratorWithTimeLimit.Run(allTriggers, ProcessTrigger);

                // Create quest objectives for all matching quest items found in the map
                //IEnumerable<LootItem> allLoot = FindObjectsOfType<LootItem>(); <-- this does not work for inactive quest items!
                IEnumerable<LootItem> allItems = Singleton<GameWorld>.Instance.LootItems.Where(i => i.Item != null).Distinct(i => i.TemplateId);
                yield return BotJobAssignmentFactory.ProcessAllQuests(QuestHelpers.LocateQuestItems, allItems);

                // Create a quest where the bots wanders to various spawn points around the map. This was implemented as a stop-gap for maps with few other quests.
                SpawnPointParams[] allSpawnPoints = Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.SpawnPointParams;
                Quest spawnPointQuest = createSpawnPointQuest(allSpawnPoints, "Spawn Point Wander", ConfigController.Config.Questing.BotQuests.SpawnPointWander);
                if (spawnPointQuest != null)
                {
                    //LoggingController.LogInfo("Adding quest for going to random spawn points...");
                    BotJobAssignmentFactory.AddQuest(spawnPointQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for going to random spawn points");
                }

                // Create a quest where initial PMC's can run to your spawn point (not directly to you).
                Models.Quest spawnRushQuest = null;
                SpawnPointParams? playerSpawnPoint = Singleton<GameWorld>.Instance.GetComponent<LocationData>().GetMainPlayerSpawnPoint();
                if (playerSpawnPoint.HasValue)
                {
                    spawnRushQuest = createGoToPositionQuest(playerSpawnPoint.Value.Position, "Spawn Rush", ConfigController.Config.Questing.BotQuests.SpawnRush);
                }
                else
                {
                    LoggingController.LogError("Cannot find player spawn point.");
                }

                if (spawnRushQuest != null)
                {
                    //LoggingController.LogInfo("Adding quest for rushing your spawn point...");
                    spawnRushQuest.PMCsOnly = true;
                    BotJobAssignmentFactory.AddQuest(spawnRushQuest);
                }
                else
                {
                    LoggingController.LogError("Could not add quest for rushing your spawn point");
                }

                // Create a quest for PMC's to go to boss spawn locations early in the raid to hunt them
                Quest bossHunterQuest = null;
                IEnumerable<string> bossZones = getBossSpawnZones();
                if (bossZones.Any())
                {
                    IEnumerable<SpawnPointParams> possibleBossSpawnPoints = allSpawnPoints.Where(s => bossZones.Contains(s.BotZoneName ?? ""));
                    bossHunterQuest = createSpawnPointQuest(possibleBossSpawnPoints, "Boss Hunter", ConfigController.Config.Questing.BotQuests.BossHunter);
                }

                if (bossHunterQuest != null)
                {
                    //LoggingController.LogInfo("Adding quest for hunting bosses...");
                    bossHunterQuest.PMCsOnly = true;
                    BotJobAssignmentFactory.AddQuest(bossHunterQuest);
                }
                else
                {
                    if (LocationScene.GetAllObjects<BotZone>(false).Count() > 1)
                    {
                        LoggingController.LogWarning("Could not add quest for hunting bosses. This is normal if bosses do not spawn on this map. Boss zones: " + string.Join(", ", bossZones));
                    }
                }

                LoadCustomQuests();

                BotJobAssignmentFactory.RemoveBlacklistedQuestObjectives(Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Id);

                // Update all other settings for EFT quests
                yield return BotJobAssignmentFactory.ProcessAllQuests(updateEFTQuestObjectives);

                HaveQuestsBeenBuilt = true;
                LoggingController.LogInfo("Finished loading quest data.");

                StartCoroutine(questPathFinder.FindStaticPathsForAllQuests());
            }
            finally
            {
                IsBuildingQuests = false;
            }
        }

        private void LoadCustomQuests()
        {
            // Load all JSON files for custom quests
            IEnumerable<Quest> customQuests = ConfigController.GetCustomQuests(Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Id);
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

        private void LoadQuest(Quest quest, IEnumerable<QuestDataClass> activeQuestsForPlayer)
        {
            quest.MaxBots = ConfigController.Config.Questing.BotQuests.EFTQuests.MaxBotsPerQuest;

            // Enumerate all zones used by the quest (in any of its objectives)
            IEnumerable<string> zoneIDs = quest.GetAllZoneIDs();

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
            quest.MinLevel = quest.GetMinLevel();
            double levelRange = ConfigController.InterpolateForFirstCol(ConfigController.Config.Questing.BotQuests.EFTQuests.LevelRange, quest.MinLevel);
            quest.MaxLevel = quest.MinLevel + (int)Math.Ceiling(levelRange);

            quest.IsActiveForPlayer = activeQuestsForPlayer.Any(q => q.Template.Id == quest.Template.Id);
            /*if (quest.IsActiveForPlayer)
            {
                LoggingController.LogInfo("Quest " + quest.Name + " is currently active for the player");
            }*/

            //LoggingController.LogInfo("Level range for quest \"" + quest.Name + " (" + quest.Template.Id + ")\": " + quest.MinLevel + "-" + quest.MaxLevel);
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

            // Find a suitable NavMesh point within the collider
            Vector3? navMeshTargetPoint = findNavMeshPointForCollider(triggerCollider, trigger.Id);
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

                float? plantTime = quest.FindPlantTime(trigger.Id);
                if (plantTime.HasValue)
                {
                    LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest.Name + " - Adding plant time: " + plantTime.Value + "s");

                    Configuration.MinMaxConfig plantTimeMinMax = new Configuration.MinMaxConfig(plantTime.Value, plantTime.Value);
                    objective.AddStep(new QuestObjectiveStep(navMeshTargetPoint.Value, QuestAction.PlantItem, plantTimeMinMax));
                    objective.LootAfterCompletingSetting = LootAfterCompleting.Inhibit;
                }

                float? beaconTime = quest.FindBeaconTime(trigger.Id);
                if (beaconTime.HasValue)
                {
                    LoggingController.LogInfo("Found trigger " + trigger.Id + " for quest: " + quest.Name + " - Adding beacon time: " + beaconTime.Value + "s");

                    objective.SetFirstWaitTimeAfterCompleting(beaconTime.Value);
                }

                zoneIDsInLocation.Add(trigger.Id);
            }

            if (ConfigController.Config.Debug.ShowZoneOutlines)
            {
                Vector3[] triggerColliderBounds = DebugHelpers.GetBoundingBoxPoints(triggerCollider.bounds);
                PathVisualizationData triggerBoundingBox = new PathVisualizationData("Trigger_" + trigger.Id, triggerColliderBounds, Color.cyan);
                Singleton<GameWorld>.Instance.GetComponent<PathRender>().AddOrUpdatePath(triggerBoundingBox);

                Vector3[] triggerTargetPoint = DebugHelpers.GetSpherePoints(navMeshTargetPoint.Value, 0.5f, 10);
                PathVisualizationData triggerTargetPosSphere = new PathVisualizationData("TriggerTargetPos_" + trigger.Id, triggerTargetPoint, Color.cyan);
                Singleton<GameWorld>.Instance.GetComponent<PathRender>().AddOrUpdatePath(triggerTargetPosSphere);
            }
        }

        private Vector3? findNavMeshPointForCollider(Collider collider, string zoneName)
        {
            bool UseNavMeshTestPoints = true;
            Vector3 targetPosition = collider.bounds.center;
            float maxSearchDistance = ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceZone;

            if (UseNavMeshTestPoints && (collider.bounds.Volume() > Math.Pow(maxSearchDistance, 3)))
            {
                float maxDensity = 0.2f;
                float density = (float)Math.Min(maxDensity, 5 / collider.bounds.extents.magnitude);

                // Generate a 3D grid of test points in the collider's bounds and check which points are on the NavMesh
                IEnumerable<Vector3> navMeshTestPoints = collider.bounds.GetNavMeshTestPoints(0.25f, density);
                IList<Vector3> navMeshPoints = navMeshTestPoints.FindPointsOnNavMesh(maxSearchDistance);

                //LoggingController.LogInfo("Generated " + navMeshTestPoints.Count() + " test points for " + zoneName + " (" + collider.bounds.Volume() + " m3) and found " + navMeshPoints.Count + " on the NavMesh");

                if (navMeshPoints.Count > 0)
                {
                    return navMeshPoints.OrderBy(p => getPointCostForCollider(collider, p)).First();
                }
            }

            // This is the old algorithm and should only be used for troubleshooting
            if (!UseNavMeshTestPoints)
            {
                // Set the target location to be in the center of the collider. If the collider is very large (i.e. for an entire building), set the
                // target location to be just above the floor.
                if (collider.bounds.extents.y > 1.5f)
                {
                    targetPosition.y = collider.bounds.min.y + 0.75f;
                    LoggingController.LogInfo("Adjusting position for zone " + zoneName + " to " + targetPosition.ToString());
                }

                // Determine how far to search for a valid NavMesh position from the target location. If the collider (zone) is very large, expand the search range.
                maxSearchDistance *= collider.bounds.Volume() > 20 ? 2 : 1;
            }

            Vector3? navMeshTargetPoint = Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindNearestNavMeshPosition(targetPosition, maxSearchDistance);
            return navMeshTargetPoint;
        }

        private static float getPointCostForCollider(Collider collider, Vector3 point)
        {
            // Initialize the cost as the distance from the center of the collider
            float cost = Vector3.Distance(collider.bounds.center, point);

            // Add an additional penalty if the point's elevation deviates from the center. The penalty should be higher if the point is
            // above the center of the collider.
            float heightFromCenter = point.y - collider.bounds.center.y;
            float heightPenaltyFactor = heightFromCenter > 0 ? 10 : 3;
            cost += Math.Abs(heightFromCenter) * heightPenaltyFactor;

            // Add an additional penalty if the selected NavMesh point isn't within the collder bounds
            if (!collider.bounds.Contains(point))
            {
                Vector3 closestPoint = collider.ClosestPointOnBounds(point);
                cost += Vector3.Distance(point, closestPoint) * 50;
            }

            return cost;
        }

        private void updateEFTQuestObjectives(Models.Quest quest)
        {
            if (!quest.IsEFTQuest)
            {
                return;
            }

            float nearbyObjectiveDistance = ConfigController.Config.Questing.BotQuests.EFTQuests.MatchLootingBehaviorDistance;
            foreach (QuestObjective objective in quest.AllObjectives)
            {
                foreach (QuestObjectiveStep step in objective.AllSteps)
                {
                    step.ChanceOfHavingKey = ConfigController.Config.Questing.BotQuests.EFTQuests.ChanceOfHavingKeys;
                }

                if (objective.LootAfterCompletingSetting == LootAfterCompleting.Inhibit)
                {
                    continue;
                }

                Vector3? objectivePosition = objective.GetFirstStepPosition();
                if (!objectivePosition.HasValue)
                {
                    continue;
                }

                // Find all nearby quest objectives that are not from EFT quests
                QuestObjective[] nearbyObjectives = BotJobAssignmentFactory.GetQuestObjectivesNearPosition(objectivePosition.Value, nearbyObjectiveDistance, false)
                    .ToArray();

                // Match the looting behavior of the nearby objectives
                if (!nearbyObjectives.Any() || nearbyObjectives.All(o => o.LootAfterCompletingSetting == LootAfterCompleting.Inhibit))
                {
                    objective.LootAfterCompletingSetting = LootAfterCompleting.Inhibit;
                    //LoggingController.LogInfo("Preventing looting after completing EFT quest objective " + objective.ToString() + " for quest " + quest.ToString());
                }
            }
        }

        private Models.Quest createGoToPositionQuest(Vector3 position, string questName, QuestSettingsConfig settings)
        {
            if (position == null)
            {
                throw new ArgumentNullException(nameof(position));
            }

            if (questName == null)
            {
                throw new ArgumentNullException(nameof(questName));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // Ensure there is a valid NavMesh position nearby
            Vector3? navMeshPosition = Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindNearestNavMeshPosition(position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
            if (!navMeshPosition.HasValue)
            {
                LoggingController.LogWarning("Cannot find NavMesh position near " + position.ToString());
                return null;
            }

            Models.Quest quest = new Models.Quest(questName);
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, settings);

            Models.QuestObjective objective = new Models.QuestObjective(navMeshPosition.Value);
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(objective, settings);
            objective.SetName(quest.Name + ": Objective #1");
            quest.AddObjective(objective);

            return quest;
        }

        private Models.Quest createSpawnPointQuest(IEnumerable<SpawnPointParams> spawnPoints, string questName, QuestSettingsConfig settings, ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All)
        {
            if (spawnPoints == null)
            {
                throw new ArgumentNullException(nameof(spawnPoints));
            }

            if (questName == null)
            {
                throw new ArgumentNullException(nameof(questName));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            // Ensure the map has spawn points matching the specified criteria
            IEnumerable<SpawnPointParams> eligibleSpawnPoints = spawnPoints.Where(s => s.Categories.Any(spawnTypes));
            if (eligibleSpawnPoints.IsNullOrEmpty())
            {
                return null;
            }

            Models.Quest quest = new Models.Quest(questName);
            QuestSettingsConfig.ApplyQuestSettingsFromConfig(quest, settings);

            int objNum = 1;
            foreach (SpawnPointParams spawnPoint in eligibleSpawnPoints)
            {
                // Ensure the spawn point has a valid nearby NavMesh position
                Vector3? navMeshPosition = Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindNearestNavMeshPosition(spawnPoint.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogWarning("Cannot find NavMesh position for spawn point " + spawnPoint.Position.ToUnityVector3().ToString());
                    continue;
                }

                Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(spawnPoint, spawnPoint.Position);
                QuestSettingsConfig.ApplyQuestSettingsFromConfig(objective, settings);
                objective.SetName(quest.Name + ": Objective #" + objNum);
                quest.AddObjective(objective);

                objNum++;
            }

            return quest;
        }

        private IEnumerable<string> getBossSpawnZones()
        {
            List<string> bossZones = new List<string>();
            foreach (BossLocationSpawn bossLocationSpawn in Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.BossLocationSpawn)
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
