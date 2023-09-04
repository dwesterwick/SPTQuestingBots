using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using UnityEngine;
using UnityEngine.AI;
using static GClass1711;

namespace SPTQuestingBots.Controllers
{
    public class LocationController : MonoBehaviour
    {
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;
        public static RaidSettings CurrentRaidSettings { get; private set; } = null;
        public static int SpawnedBotCount { get; set; } = 0;
        public static int SpawnedBossCount { get; set; } = 0;
        public static int SpawnedRogueCount { get; set; } = 0;
        public static int SpawnedBossWaves { get; set; } = 0;
        public static int ZeroWaveCount { get; set; } = 0;
        public static int ZeroWaveTotalBotCount { get; set; } = 0;
        public static int ZeroWaveTotalRogueCount { get; set; } = 0;

        private static TarkovApplication tarkovApplication = null;
        private static LocationSettingsClass locationSettings = null;
        private static Dictionary<string, int> originalEscapeTimes = new Dictionary<string, int>();
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static LootableContainer[] lootableContainers = new LootableContainer[0];
        private static List<LootItem> lootItems = new List<LootItem>();
        private static List<BotOwner> spawnedBosses = new List<BotOwner>();

        private static void Clear()
        {
            SpawnedBossWaves = 0;
            SpawnedBotCount = 0;
            SpawnedBossCount = 0;
            SpawnedRogueCount = 0;
            ZeroWaveCount = 0;
            ZeroWaveTotalBotCount = 0;
            ZeroWaveTotalRogueCount = 0;

            spawnedBosses.Clear();
            lootItems.Clear();
            lootableContainers = new LootableContainer[0];
            CurrentLocation = null;
            CurrentRaidSettings = null;
            nearestNavMeshPoint.Clear();
        }

        private void Update()
        {
            if (originalEscapeTimes.Count == 0)
            {
                tarkovApplication = FindObjectOfType<TarkovApplication>();
                if (tarkovApplication == null)
                {
                    return;
                }

                locationSettings = getLocationSettings(tarkovApplication);
                if (locationSettings == null)
                {
                    return;
                }

                foreach (string location in locationSettings.locations.Keys)
                {
                    originalEscapeTimes.Add(locationSettings.locations[location].Id, locationSettings.locations[location].EscapeTimeLimit);
                }
            }

            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                if (CurrentLocation != null)
                {
                    LoggingController.LogInfo("Clearing location data...");
                    Clear();
                }

                return;
            }

            if (CurrentLocation == null)
            {
                CurrentRaidSettings = getCurrentRaidSettings();
                CurrentLocation = CurrentRaidSettings.SelectedLocation;
                if (CurrentLocation == null)
                {
                    return;
                }

                lootableContainers = FindObjectsOfType<LootableContainer>();
                LoggingController.LogInfo("Found " + lootableContainers.Length + " lootable containers in the map");
            }
        }

        public static bool TryGetObjectNearPosition<T>(Vector3 position, float maxDistance, bool onlyVisible, out T obj) where T: Behaviour
        {
            obj = null;

            if (LocationScene.LoadedScenes.Count == 0)
            {
                return false;
            }

            foreach (T item in LocationScene.GetAllObjects<T>(true))
            {
                float distace = Vector3.Distance(item.transform.position, position);
                if (distace <= maxDistance)
                {
                    if (onlyVisible)
                    {
                        float rayEndPointThreshold = 0.02f;
                        Vector3 direction = item.transform.position - position;
                        RaycastHit[] raycastHits = Physics.RaycastAll(position, direction, distace, LayerMaskClass.HighPolyWithTerrainMask);
                        IEnumerable<RaycastHit> filteredRaycastHits = raycastHits
                            .Where(r => r.distance > distace * rayEndPointThreshold)
                            .Where(r => r.distance < distace * (1 - rayEndPointThreshold));

                        if (filteredRaycastHits.Any())
                        {
                            //IEnumerable<string> raycastDataText = filteredRaycastHits.Select(h => h.collider.name + " (" + h.distance + "/" + distace + ")");
                            //LoggingController.LogInfo("Ignoring object " + item.GetType().Name + " at " + item.transform.position.ToString() + " due to raycast hits at: " + string.Join(", ", raycastDataText));
                            continue;
                        }
                    }

                    obj = item;
                    return true;
                }
            }

            return false;
        }

        public static void RegisterBot(BotOwner botOwner)
        {
            SpawnedBotCount++;
            string message = "Spawned ";

            if ((BotGenerator.SpawnedInitialPMCCount == 0) && !BotGenerator.IsSpawningPMCs)
            {
                spawnedBosses.Add(botOwner);
                message += "boss " + botOwner.Profile.Nickname + " (" + spawnedBosses.Count + "/" + ZeroWaveTotalBotCount + ")";
            }
            else
            {
                message += "bot #" + SpawnedBotCount + ": " + botOwner.Profile.Nickname;
            }

            message += " (" + botOwner.Side + ")";
            LoggingController.LogInfo(message);
        }

        public static LootableContainer GetNearestLootableContainer(BotOwner botOwner)
        {
            if (lootableContainers.Length == 0)
            {
                return null;
            }

            IEnumerable<LootableContainer> sortedContainers = lootableContainers.OrderBy(l => Vector3.Distance(botOwner.Position, l.transform.position));
            return sortedContainers.First();
        }

        public static float GetDistanceToNearestLootableContainer(BotOwner botOwner)
        {
            LootableContainer nearestContainer = GetNearestLootableContainer(botOwner);
            if (nearestContainer == null)
            {
                return float.MaxValue;
            }

            return Vector3.Distance(botOwner.Position, nearestContainer.transform.position);
        }

        public static Vector3? GetPlayerPosition()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        public static float? GetOriginalEscapeTime()
        {
            return GetOriginalEscapeTime(CurrentLocation?.Id);
        }

        public static float? GetOriginalEscapeTime(string locationID)
        {
            if (!originalEscapeTimes.ContainsKey(locationID))
            {
                LoggingController.LogError("Could not get original escape time for location " + locationID);
                return null;
            }

            return originalEscapeTimes[locationID];
        }

        public static float? GetCurrentEscapeTime()
        {
            return CurrentLocation?.EscapeTimeLimit;
        }

        public static float? GetRemainingRaidTime()
        {
            if (Singleton<AbstractGame>.Instance == null)
            {
                return null;
            }

            float? escapeTime = GetCurrentEscapeTime();
            if (!escapeTime.HasValue)
            {
                LoggingController.LogError("Could not determine remaining raid time");
                return null;
            }

            float remainingTimeFromGame = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);

            return Math.Min(remainingTimeFromGame, escapeTime.Value * 60f);
        }

        public static float? GetElapsedRaidTime()
        {
            return GetElapsedRaidTime(CurrentLocation?.Id);
        }

        public static float? GetElapsedRaidTime(string locationID)
        {
            float? remainingTime = GetRemainingRaidTime();
            float? originalEscapeTime = GetOriginalEscapeTime(locationID);

            if (!remainingTime.HasValue || !originalEscapeTime.HasValue)
            {
                LoggingController.LogError("Could not calculate elapsed raid time");
                return null;
            }

            return (originalEscapeTime.Value * 60f) - remainingTime.Value;
        }

        public static float? GetRaidTimeRemainingFraction()
        {
            return GetRaidTimeRemainingFraction(CurrentLocation?.Id);
        }

        public static float? GetRaidTimeRemainingFraction(string locationID)
        {
            float? remainingTime = GetRemainingRaidTime();
            float? originalEscapeTime = GetOriginalEscapeTime(locationID);

            if (!remainingTime.HasValue || !originalEscapeTime.HasValue)
            {
                LoggingController.LogError("Could not calculate elapsed raid time");
                return null;
            }

            return remainingTime.Value / (originalEscapeTime * 60f);
        }

        public static Models.Quest CreateSpawnPointQuest(ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All)
        {
            IEnumerable<SpawnPointParams> eligibleSpawnPoints = CurrentLocation.SpawnPointParams.Where(s => s.Categories.Any(spawnTypes));
            if (eligibleSpawnPoints.IsNullOrEmpty())
            {
                return null;
            }

            Models.Quest quest = new Models.Quest(ConfigController.Config.BotQuests.SpawnPointWander.Priority, "Spawn Points");
            quest.ChanceForSelecting = ConfigController.Config.BotQuests.SpawnPointWander.Chance;
            foreach (SpawnPointParams spawnPoint in eligibleSpawnPoints)
            {
                Vector3? navMeshPosition = FindNearestNavMeshPosition(spawnPoint.Position, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogWarning("Cannot find NavMesh position for spawn point " + spawnPoint.Position.ToUnityVector3().ToString());
                    continue;
                }

                Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(spawnPoint, spawnPoint.Position);
                objective.MaxBots = ConfigController.Config.BotQuests.SpawnPointWander.MaxBotsPerQuest;
                objective.MinDistanceFromBot = ConfigController.Config.BotQuests.SpawnPointWander.MinDistance;
                quest.AddObjective(objective);
            }

            return quest;
        }

        public static Models.Quest CreateSpawnRushQuest()
        {
            SpawnPointParams? playerSpawnPoint = getPlayerSpawnPoint();
            if (!playerSpawnPoint.HasValue)
            {
                LoggingController.LogWarning("Cannot find player spawn point.");
                return null;
            }

            Vector3? navMeshPosition = FindNearestNavMeshPosition(playerSpawnPoint.Value.Position, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceSpawn);
            if (!navMeshPosition.HasValue)
            {
                LoggingController.LogWarning("Cannot find NavMesh position for player spawn point.");
                return null;
            }

            //Vector3? playerPosition = GetPlayerPosition();
            //LoggingController.LogInfo("Creating spawn rush quest for " + playerSpawnPoint.Value.Id + " via " + navMeshPosition.Value.ToString() + " for player at " + playerPosition.Value.ToString() + "...");

            Models.Quest quest = new Models.Quest(ConfigController.Config.BotQuests.SpawnRush.Priority, "Spawn Rush");
            quest.ChanceForSelecting = ConfigController.Config.BotQuests.SpawnRush.Chance;
            quest.MaxRaidET = ConfigController.Config.BotQuests.SpawnRush.MaxRaidET;

            Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(playerSpawnPoint.Value, navMeshPosition.Value);
            objective.MaxDistanceFromBot = ConfigController.Config.BotQuests.SpawnRush.MaxDistance;
            objective.MaxBots = ConfigController.Config.BotQuests.SpawnRush.MaxBotsPerQuest;

            quest.AddObjective(objective);
            return quest;
        }

        public static Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
        {
            if (nearestNavMeshPoint.ContainsKey(position))
            {
                return nearestNavMeshPoint[position];
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit sourceNearestPoint, searchDistance, NavMesh.AllAreas))
            {
                nearestNavMeshPoint.Add(position, sourceNearestPoint.position);
                return sourceNearestPoint.position;
            }

            return null;
        }

        public static SpawnPointParams GetFurthestSpawnPoint(Vector3[] referencePositions, SpawnPointParams[] allSpawnPoints)
        {
            if (referencePositions.Length == 0)
            {
                throw new ArgumentException("The reference position array is empty.", "referencePositions");
            }

            if (allSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The spawn-point array is empty.", "allSpawnPoints");
            }

            Dictionary<SpawnPointParams, float> nearestReferencePoints = new Dictionary<SpawnPointParams, float>();
            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                float nearestDistance = Vector3.Distance(referencePositions[0], allSpawnPoints[s].Position.ToUnityVector3());

                for (int b = 1; b < referencePositions.Length; b++)
                {
                    float distance = Vector3.Distance(referencePositions[b], allSpawnPoints[s].Position.ToUnityVector3());

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                    }
                }

                nearestReferencePoints.Add(allSpawnPoints[s], nearestDistance);
            }

            return nearestReferencePoints.OrderBy(p => p.Value).Last().Key;
        }

        public static SpawnPointParams GetFurthestSpawnPoint(SpawnPointParams[] referenceSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            if (referenceSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The reference spawn-point array is empty.", "referenceSpawnPoints");
            }

            if (allSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The spawn-point array is empty.", "allSpawnPoints");
            }

            Dictionary<SpawnPointParams, float> nearestReferencePoints = new Dictionary<SpawnPointParams, float>();
            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                SpawnPointParams nearestSpawnPoint = referenceSpawnPoints[0];
                float nearestDistance = Vector3.Distance(referenceSpawnPoints[0].Position.ToUnityVector3(), allSpawnPoints[s].Position.ToUnityVector3());

                for (int r = 1; r < referenceSpawnPoints.Length; r++)
                {
                    float distance = Vector3.Distance(referenceSpawnPoints[r].Position.ToUnityVector3(), allSpawnPoints[s].Position.ToUnityVector3());

                    if (distance < nearestDistance)
                    {
                        nearestSpawnPoint = referenceSpawnPoints[r];
                        nearestDistance = distance;
                    }
                }

                nearestReferencePoints.Add(allSpawnPoints[s], nearestDistance);
            }

            return nearestReferencePoints.OrderBy(p => p.Value).Last().Key;
        }

        public static SpawnPointParams GetFurthestSpawnPoint(Vector3 postition, SpawnPointParams[] allSpawnPoints)
        {
            SpawnPointParams furthestSpawnPoint = allSpawnPoints[0];
            float furthestDistance = Vector3.Distance(postition, furthestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());

                if (distance > furthestDistance)
                {
                    furthestSpawnPoint = allSpawnPoints[s];
                    furthestDistance = distance;
                }
            }

            return furthestSpawnPoint;
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            SpawnPointParams nearestSpawnPoint = allSpawnPoints[0];
            float nearestDistance = Vector3.Distance(postition, nearestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                if (excludedSpawnPoints.Any(p => p.Id == allSpawnPoints[s].Id))
                {
                    continue;
                }

                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());

                if (distance < nearestDistance)
                {
                    nearestSpawnPoint = allSpawnPoints[s];
                    nearestDistance = distance;
                }
            }

            return nearestSpawnPoint;
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition)
        {
            return GetNearestSpawnPoint(postition, new SpawnPointParams[0], CurrentLocation.SpawnPointParams);
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints)
        {
            return GetNearestSpawnPoint(postition, excludedSpawnPoints, CurrentLocation.SpawnPointParams);
        }

        private static SpawnPointParams? getPlayerSpawnPoint()
        {
            Vector3? playerPosition = GetPlayerPosition();
            if (!playerPosition.HasValue)
            {
                return null;
            }

            return GetNearestSpawnPoint(playerPosition.Value);
        }

        private static LocationSettingsClass getLocationSettings(TarkovApplication app)
        {
            if (app == null)
            {
                LoggingController.LogError("Invalid Tarkov application instance");
                return null;
            }

            ISession session = app.GetClientBackEndSession();
            if (session == null)
            {
                return null;
            }

            return session.LocationSettings;
        }

        private static RaidSettings getCurrentRaidSettings()
        {
            if (tarkovApplication == null)
            {
                LoggingController.LogError("Invalid Tarkov application instance");
                return null;
            }

            FieldInfo raidSettingsField = typeof(TarkovApplication).GetField("_raidSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            RaidSettings raidSettings = raidSettingsField.GetValue(tarkovApplication) as RaidSettings;
            return raidSettings;
        }
    }
}
