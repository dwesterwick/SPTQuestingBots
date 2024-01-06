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

namespace SPTQuestingBots.Controllers
{
    public class LocationController : MonoBehaviour
    {
        public static bool IsScavRun { get; set; } = false;
        public static bool HasRaidStarted { get; set; } = false;
        public static LocationSettingsClass.Location CurrentLocation { get; private set; } = null;
        public static RaidSettings CurrentRaidSettings { get; private set; } = null;
        
        private static TarkovApplication tarkovApplication = null;
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static Dictionary<string, EFT.Interactive.Switch> switches = new Dictionary<string, EFT.Interactive.Switch>();
        private static Dictionary<Door, bool> areLockedDoorsUnlocked = new Dictionary<Door, bool>();
        private static Dictionary<Door, Vector3> doorInteractionPositions = new Dictionary<Door, Vector3>();
        private static float maxExfilPointDistance = 0;

        private static void Clear()
        {
            CurrentLocation = null;
            CurrentRaidSettings = null;
            nearestNavMeshPoint.Clear();
            switches.Clear();
            areLockedDoorsUnlocked.Clear();
            doorInteractionPositions.Clear();
            maxExfilPointDistance = 0;
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
                if (!door.Operatable)
                {
                    //LoggingController.LogInfo("Door " + door.Id + " is inoperable");
                    continue;
                }

                if (!door.CanBeBreached && (door.KeyId == ""))
                {
                    //LoggingController.LogInfo("Door " + door.Id + " cannot be breached and has no valid key");
                    continue;
                }

                if (door.DoorState != EDoorState.Locked)
                {
                    continue;
                }

                areLockedDoorsUnlocked.Add(door, false);
            }

            LoggingController.LogInfo("Found " + areLockedDoorsUnlocked.Count + " locked doors");
            //LoggingController.LogInfo("Found locked doors: " + string.Join(", ", areLockedDoorsUnlocked.Select(s => s.Key.Id)));
        }

        public static IEnumerable<Door> FindLockedDoorsNearPosition(Vector3 position, float maxDistance, bool stillLocked = true)
        {
            Dictionary<Door, float> lockedDoorsAndDistance = new Dictionary<Door, float>();

            foreach (Door door in areLockedDoorsUnlocked.Keys.ToArray())
            {
                // Check if the door has been unlocked since this method was previously called
                if (!areLockedDoorsUnlocked[door] && (door.DoorState != EDoorState.Locked))
                {
                    LoggingController.LogInfo("Door " + door.Id + " is no longer locked.");
                    areLockedDoorsUnlocked[door] = true;
                }

                // Check if the door is within the desired distance
                float distance = Vector3.Distance(position, door.transform.position);
                if (distance > maxDistance)
                {
                    continue;
                }

                // Check if the door is locked
                if (stillLocked && !areLockedDoorsUnlocked[door])
                {
                    lockedDoorsAndDistance.Add(door, distance);
                }

                // Check if the door is unlocked
                if (!stillLocked && areLockedDoorsUnlocked[door])
                {
                    lockedDoorsAndDistance.Add(door, distance);
                }
            }

            // Sort the matching doors based on distance to the position
            return lockedDoorsAndDistance.OrderBy(d => d.Value).Select(d => d.Key);
        }

        public static void ReportUnlockedDoor(Door door)
        {
            if (areLockedDoorsUnlocked.ContainsKey(door))
            {
                areLockedDoorsUnlocked[door] = true;
            }
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
            KeyValuePair<SpawnPointParams, float> selectedPoint = nearestReferencePoints.OrderBy(p => p.Value).Last();

            LoggingController.LogInfo("Found furthest spawn point " + selectedPoint.Key.Position.ToUnityVector3().ToString() + " that is " + selectedPoint.Value + "m from other players");

            return selectedPoint.Key;
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
                if ((distance < nearestDistance) || excludedSpawnPoints.Contains(nearestSpawnPoint))
                {
                    nearestSpawnPoint = allSpawnPoints[s];
                    nearestDistance = distance;
                }
            }

            // Ensure at least one possible spawn point hasn't also been excluded
            if (excludedSpawnPoints.Contains(nearestSpawnPoint))
            {
                throw new InvalidOperationException("All possible spawn points (" + allSpawnPoints.Length + ") are in the blacklist (" + excludedSpawnPoints.Length + ")");
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

        public static Vector3? GetDoorInteractionPosition(Door door, Vector3 startingPosition)
        {
            // If a cached position exists, return it
            if (doorInteractionPositions.ContainsKey(door))
            {
                return doorInteractionPositions[door];
            }

            Dictionary<Vector3, float> validPositions = new Dictionary<Vector3, float>();

            // Determine positions around the door to test
            float searchDistance = ConfigController.Config.Questing.UnlockingDoors.DoorApproachPositionSearchRadius;
            float searchOffset = ConfigController.Config.Questing.UnlockingDoors.DoorApproachPositionSearchOffset;
            Vector3[] possibleInteractionPositions = new Vector3[4]
            {
                door.transform.position + new Vector3(searchDistance, 0, 0) + new Vector3(0, searchOffset, 0),
                door.transform.position - new Vector3(searchDistance, 0, 0) + new Vector3(0, searchOffset, 0),
                door.transform.position + new Vector3(0, 0, searchDistance) + new Vector3(0, searchOffset, 0),
                door.transform.position - new Vector3(0, 0, searchDistance) + new Vector3(0, searchOffset, 0)
            };

            // Test each position
            foreach (Vector3 possibleInteractionPosition in possibleInteractionPositions)
            {
                // Determine if a valid NavMesh location can be found for the position
                Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(possibleInteractionPosition, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceDoors);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogInfo("Cannot access position " + possibleInteractionPosition.ToString() + " for door " + door.Id);

                    if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                    {
                        PathRender.outlinePosition(possibleInteractionPosition, Color.white, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceDoors);
                    }

                    continue;
                }

                //LoggingController.LogInfo(BotOwner.GetText() + " is checking the accessibility of position " + navMeshPosition.Value.ToString() + " for door " + door.Id + "...");

                // Try to calculate a path from the bot to the NavMesh location identified for the position
                NavMeshPath path = new NavMeshPath();
                NavMesh.CalculatePath(startingPosition, navMeshPosition.Value, NavMesh.AllAreas, path);

                // Check if the bot is able to reach the NavMesh location identified for the position
                if (path.status == NavMeshPathStatus.PathComplete)
                {
                    validPositions.Add(navMeshPosition.Value, Vector3.Distance(navMeshPosition.Value, door.transform.position));
                    continue;
                }

                if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                {
                    PathRender.outlinePosition(navMeshPosition.Value, Color.yellow);
                }
            }

            // Check if there are any positions around the door that the bot is able to reach
            if (validPositions.Count > 0)
            {
                // Sort the positions based on their poximity to the door
                IEnumerable<Vector3> orderedPostions = validPositions.OrderBy(p => p.Value).Select(p => p.Key);

                // If applicable, draw the positions in the world
                if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                {
                    PathRender.outlinePosition(orderedPostions.First(), Color.green);

                    foreach (Vector3 alternatePosition in orderedPostions.Skip(1))
                    {
                        PathRender.outlinePosition(alternatePosition, Color.magenta);
                    }
                }

                // Select the position closest to the door
                Vector3 interactionPosition = orderedPostions.First();

                // Cache the position and return it
                doorInteractionPositions.Add(door, interactionPosition);
                return interactionPosition;
            }

            return null;
        }

        public static Door FindFirstAccessibleDoor(IEnumerable<Door> doors, Vector3 startingPosition)
        {
            foreach (Door door in doors)
            {
                Vector3? interactionPosition = GetDoorInteractionPosition(door, startingPosition);
                if (interactionPosition.HasValue)
                {
                    return door;
                }
            }

            return null;
        }

        public static float GetMaxExfilPointDistance()
        {
            if (maxExfilPointDistance > 0)
            {
                return maxExfilPointDistance;
            }

            float maxDistance = 0;
            foreach (ExfiltrationPoint firstPoint in Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints)
            {
                foreach (ExfiltrationPoint secondPoint in Singleton<GameWorld>.Instance.ExfiltrationController.ExfiltrationPoints)
                {
                    float distance = Vector3.Distance(firstPoint.transform.position, secondPoint.transform.position);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }

            maxExfilPointDistance = maxDistance;
            return maxExfilPointDistance;
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
