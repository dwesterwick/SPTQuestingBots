using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using GameTimerHelpers = GClass1368;

namespace SPTQuestingBots.Controllers
{
    public class LocationController : MonoBehaviour
    {
        public static bool IsScavRun { get; set; } = false;
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;
        public static RaidSettings CurrentRaidSettings { get; private set; } = null;
        
        private static TarkovApplication tarkovApplication = null;
        private static Dictionary<string, int> originalEscapeTimes = new Dictionary<string, int>();
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static Dictionary<string, EFT.Interactive.Switch> switches = new Dictionary<string, EFT.Interactive.Switch>();
        private static Dictionary<Door, bool> areLockedDoorsUnlocked = new Dictionary<Door, bool>();

        private static void Clear()
        {
            CurrentLocation = null;
            CurrentRaidSettings = null;
            nearestNavMeshPoint.Clear();
            switches.Clear();
            areLockedDoorsUnlocked.Clear();
        }

        private void Update()
        {
            if (tarkovApplication == null)
            {
                tarkovApplication = FindObjectOfType<TarkovApplication>();
                return;
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

                CurrentLocation = CurrentRaidSettings?.SelectedLocation;
                if (CurrentLocation == null)
                {
                    return;
                }

                LoggingController.LogInfo("Loading into " + CurrentLocation.Id + "...");
            }
        }

        public static void FindAllInteractiveObjects()
        {
            FindAllSwitches();
            FindAllLockedDoors();
        }

        public static void FindAllSwitches()
        {
            switches.Clear();

            EFT.Interactive.Switch[] allSwitches = FindObjectsOfType<EFT.Interactive.Switch>();
            switches.AddRange(allSwitches.ToDictionary(s => s.Id, s => s));
            
            //LoggingController.LogInfo("Found switches: " + string.Join(", ", allSwitches.Select(s => s.Id)));

            foreach (EFT.Interactive.Switch sw in allSwitches)
            {
                sw.OnDoorStateChanged += reportSwitchChange;
            }
        }

        private static void reportSwitchChange(WorldInteractiveObject obj, EDoorState prevState, EDoorState nextState)
        {
            LoggingController.LogInfo("Switch " + obj.Id + " has changed from " + prevState.ToString() + " to " + nextState.ToString() + ". Interacting Player: " + (obj.InteractingPlayer?.Profile?.Nickname ?? "(none)"));
        }

        public static EFT.Interactive.Switch FindSwitch(string id)
        {
            if (switches.ContainsKey(id))
            {
                return switches[id];
            }

            return null;
        }

        public static void FindAllLockedDoors()
        {
            areLockedDoorsUnlocked.Clear();

            Door[] allDoors = FindObjectsOfType<Door>();
            foreach (Door door in allDoors)
            {
                if (door.DoorState != EDoorState.Locked)
                {
                    continue;
                }

                if (!door.Operatable)
                {
                    LoggingController.LogInfo("Door " + door.Id + " is inoperable");
                    continue;
                }

                if (!door.CanBeBreached && (door.KeyId == ""))
                {
                    LoggingController.LogInfo("Door " + door.Id + " cannot be breached and has no valid key");
                    continue;
                }

                areLockedDoorsUnlocked.Add(door, false);
            }

            //LoggingController.LogInfo("Found locked doors: " + string.Join(", ", lockedDoors.Select(s => s.Id)));
        }

        public static IEnumerable<Door> FindLockedDoorsNearPosition(Vector3 position, float maxDistance, bool stillLocked = true)
        {
            Dictionary<Door, float> lockedDoorsAndDistance = new Dictionary<Door, float>();
            foreach (Door door in areLockedDoorsUnlocked.Keys.ToArray())
            {
                if (!areLockedDoorsUnlocked[door] && (door.DoorState != EDoorState.Locked))
                {
                    LoggingController.LogInfo("Door " + door.Id + " is no longer locked.");
                    areLockedDoorsUnlocked[door] = true;
                }

                float distance = Vector3.Distance(position, door.transform.position);
                if (distance > maxDistance)
                {
                    continue;
                }

                if (stillLocked && !areLockedDoorsUnlocked[door])
                {
                    lockedDoorsAndDistance.Add(door, distance);
                }

                if (!stillLocked && areLockedDoorsUnlocked[door])
                {
                    lockedDoorsAndDistance.Add(door, distance);
                }
            }

            return lockedDoorsAndDistance.OrderBy(d => d.Value).Select(d => d.Key);
        }

        public static SpawnPointParams[] GetAllValidSpawnPointParams()
        {
            if (CurrentLocation == null)
            {
                throw new InvalidOperationException("Spawn-point data can only be retrieved after a map has been loaded.");
            }

            if (CurrentLocation.Id == "TarkovStreets")
            {
                SpawnPointParams[] validSpawnPointParams = CurrentLocation.SpawnPointParams
                    .Where(s => s.Position.z < 440)
                    .ToArray();

                //IEnumerable<SpawnPointParams> removedSpawnPoints = CurrentLocation.SpawnPointParams.Where(s => !validSpawnPointParams.Contains(s));
                //string removedSpawnPointsText = string.Join(", ", removedSpawnPoints.Select(s => s.Position.ToUnityVector3().ToString()));
                //Controllers.LoggingController.LogWarning("PMC's cannot spawn south of the cinema on Streets or their minds will be broken. Thanks, BSG! Removed spawn points: " + removedSpawnPointsText);

                return validSpawnPointParams;
            }

            return CurrentLocation.SpawnPointParams;
        }

        public static void ClearEscapeTimes()
        {
            LoggingController.LogInfo("Clearing cached escape times...");
            originalEscapeTimes.Clear();
            HasRaidStarted = false;
        }

        public static void CacheEscapeTimes()
        {
            // Check if escape-time data has already been cached
            if (originalEscapeTimes.Count > 0)
            {
                return;
            }

            if (tarkovApplication == null)
            {
                throw new InvalidOperationException("Location settings cannot be retrieved.");
            }

            LocationSettingsClass locationSettings = getLocationSettings(tarkovApplication);
            if (locationSettings == null)
            {
                throw new InvalidOperationException("Location settings could not be retrieved.");
            }

            LoggingController.LogInfo("Caching escape times...");

            foreach (string location in locationSettings.locations.Keys)
            {
                originalEscapeTimes.Add(locationSettings.locations[location].Id, locationSettings.locations[location].EscapeTimeLimit);

                if (originalEscapeTimes.Last().Value > 0)
                {
                    LoggingController.LogInfo("Caching escape times..." + originalEscapeTimes.Last().Key + ": " + originalEscapeTimes.Last().Value);
                }
            }

            LoggingController.LogInfo("Caching escape times...done.");

            HasRaidStarted = false;
        }

        // This isn't actually used anywhere in this mod, but I left it in here because it's a pretty nifty algorithm
        public static bool TryGetObjectNearPosition<T>(Vector3 position, float maxDistance, bool onlyVisible, out T obj) where T: Behaviour
        {
            obj = null;

            // Ensure a map has been loaded in the game
            if (LocationScene.LoadedScenes.Count == 0)
            {
                return false;
            }

            // Find all objects of the desired type in the map
            foreach (T item in LocationScene.GetAllObjects<T>(true))
            {
                // Check if the object is close enough
                float distace = Vector3.Distance(item.transform.position, position);
                if (distace > maxDistance)
                {
                    continue;
                }

                // If the object needs to be visible from the source position, perform additional checks
                if (onlyVisible)
                {
                    // Perform a raycast test from the source position to the object
                    Vector3 direction = item.transform.position - position;
                    RaycastHit[] raycastHits = Physics.RaycastAll(position, direction, distace, LayerMaskClass.HighPolyWithTerrainMask);

                    // Ignore raycast hits that are very close to the source position or the object
                    float rayEndPointThreshold = 0.02f;
                    IEnumerable<RaycastHit> filteredRaycastHits = raycastHits
                        .Where(r => r.distance > distace * rayEndPointThreshold)
                        .Where(r => r.distance < distace * (1 - rayEndPointThreshold));

                    // If there are any remaining raycast hits, assume the object is not visible
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

            return false;
        }

        public static Vector3? GetPlayerPosition()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        public static SpawnPointParams? GetPlayerSpawnPoint()
        {
            Vector3? playerPosition = GetPlayerPosition();
            if (!playerPosition.HasValue)
            {
                return null;
            }

            return GetNearestSpawnPoint(playerPosition.Value);
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

            float remainingTimeFromGame = GameTimerHelpers.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);

            // Until the raid starts, remainingTimeFromGame is a very high number, so it needs to be reduced to the actual starting raid time
            return Math.Min(remainingTimeFromGame, escapeTime.Value * 60f);
        }

        public static float? GetTimeSinceSpawning()
        {
            float? remainingTime = GetRemainingRaidTime();
            int? escapeTime = CurrentLocation?.EscapeTimeLimit;

            if (!remainingTime.HasValue || !escapeTime.HasValue)
            {
                LoggingController.LogError("Could not calculate time since spawning");
                return null;
            }

            return (escapeTime.Value * 60f) - remainingTime.Value;
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

        public static Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
        {
            // Check if there is a cached value for the position, and if so return it
            if (nearestNavMeshPoint.ContainsKey(position))
            {
                return nearestNavMeshPoint[position];
            }

            if (NavMesh.SamplePosition(position, out NavMeshHit sourceNearestPoint, searchDistance, NavMesh.AllAreas))
            {
                // Cache the result to make subsequent calls for the position faster
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

                // Search for the nearest reference position to the spawn point
                for (int b = 1; b < referencePositions.Length; b++)
                {
                    float distance = Vector3.Distance(referencePositions[b], allSpawnPoints[s].Position.ToUnityVector3());

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                    }
                }

                // For each spawn point, store the distance to the nearest reference point
                nearestReferencePoints.Add(allSpawnPoints[s], nearestDistance);
            }

            // The furthest spawn point from all reference positions is the one that has the furthest minimum distance to all of them
            return nearestReferencePoints.OrderBy(p => p.Value).Last().Key;
        }

        public static SpawnPointParams GetFurthestSpawnPoint(SpawnPointParams[] referenceSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            return GetFurthestSpawnPoint(referenceSpawnPoints.Select(p => p.Position.ToUnityVector3()).ToArray(), allSpawnPoints);
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            if (allSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The spawn-point array is empty.", "allSpawnPoints");
            }

            // Select the first spawn point by default
            SpawnPointParams nearestSpawnPoint = allSpawnPoints[0];
            float nearestDistance = Vector3.Distance(postition, nearestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                // Check if the spawn point is allowed to be selected
                if (excludedSpawnPoints.Any(p => p.Id == allSpawnPoints[s].Id))
                {
                    continue;
                }

                // Check if the spawn point is closer than the previous one selected
                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());
                if (distance < nearestDistance)
                {
                    nearestSpawnPoint = allSpawnPoints[s];
                    nearestDistance = distance;
                }
            }

            // Ensure at least one possible spawn point hasn't also been excluded
            if (excludedSpawnPoints.Contains(nearestSpawnPoint))
            {
                throw new InvalidOperationException("All possible spawn points are excluded.");
            }

            return nearestSpawnPoint;
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition)
        {
            return GetNearestSpawnPoint(postition, new SpawnPointParams[0], GetAllValidSpawnPointParams());
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints)
        {
            return GetNearestSpawnPoint(postition, excludedSpawnPoints, GetAllValidSpawnPointParams());
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
