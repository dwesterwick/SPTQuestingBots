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
using HarmonyLib;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Components
{
    public class LocationData : MonoBehaviour
    {
        public int MaxTotalBots { get; private set; } = 15;
        public float MaxDistanceBetweenSpawnPoints { get; private set; } = float.MaxValue;
        public LocationSettingsClass.Location CurrentLocation { get; private set; } = null;
        public RaidSettings CurrentRaidSettings { get; private set; } = null;

        private readonly DateTime awakeTime = DateTime.Now;
        private GamePlayerOwner gamePlayerOwner = null;
        private LightkeeperIslandMonitor lightkeeperIslandMonitor = null;
        private Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private Dictionary<string, EFT.Interactive.Switch> switches = new Dictionary<string, EFT.Interactive.Switch>();
        private Dictionary<string, WorldInteractiveObject> IDsForWorldInteractiveObjects = new Dictionary<string, WorldInteractiveObject>();
        private Dictionary<WorldInteractiveObject, bool> areLockedDoorsUnlocked = new Dictionary<WorldInteractiveObject, bool>();
        private Dictionary<WorldInteractiveObject, Vector3> doorInteractionPositions = new Dictionary<WorldInteractiveObject, Vector3>();
        private Dictionary<WorldInteractiveObject, NoPowerTip> noPowerTipsForDoors = new Dictionary<WorldInteractiveObject, NoPowerTip>();
        private float maxExfilPointDistance = 0;

        protected void Awake()
        {
            gamePlayerOwner = FindObjectOfType<GamePlayerOwner>();

            CurrentRaidSettings = FindObjectOfType<QuestingBotsPlugin>().GetComponent<TarkovData>().GetCurrentRaidSettings();
            if (CurrentRaidSettings == null)
            {
                LoggingController.LogError("Could not retrieve current raid settings");
            }

            PathRenderer pathRender = Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<PathRenderer>();

            CurrentLocation = CurrentRaidSettings.SelectedLocation;
            if (CurrentLocation.Id == "Lighthouse")
            {
                lightkeeperIslandMonitor = Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<LightkeeperIslandMonitor>();
            }

            UpdateMaxTotalBots();

            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<BotLogic.HiveMind.BotHiveMindMonitor>();

            if (ConfigController.Config.Questing.Enabled)
            {
                QuestHelpers.ClearCache();
                Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<BotQuestBuilder>();
                Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<DebugData>();
            }

            if (ConfigController.Config.BotSpawns.Enabled)
            {
                BotGenerator.RunBotGenerationTasks();
            }

            calculateMaxDistanceBetweenSpawnPoints();
            BotObjectiveManagerFactory.Clear();
        }

        protected void Update()
        {
            handleCustomQuestKeypress();
        }

        public void UpdateMaxTotalBots()
        {
            BotsController botControllerClass = Singleton<IBotGame>.Instance.BotsController;
            int botmax = botControllerClass._maxCount;
            if (botmax > 0)
            {
                MaxTotalBots = botmax;
            }
            
            //LoggingController.LogInfo("Max total bots on the map (" + CurrentLocation.Id + ") at the same time: " + MaxTotalBots);
        }

        public bool IsPointOnLightkeeperIsland(Vector3 position)
        {
            if (lightkeeperIslandMonitor == null)
            {
                return false;
            }

            return lightkeeperIslandMonitor.IsPointOnLightkeeperIsland(position);
        }

        public void FindAllInteractiveObjects()
        {
            FindAllSwitches();
            FindAllLockedDoors();
        }

        public void FindAllSwitches()
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

        private void reportSwitchChange(WorldInteractiveObject obj, EDoorState prevState, EDoorState nextState)
        {
            LoggingController.LogInfo("Switch " + obj.Id + " has changed from " + prevState.ToString() + " to " + nextState.ToString() + ". Interacting Player: " + (obj.InteractingPlayer?.Profile?.Nickname ?? "(none)"));
        }

        public EFT.Interactive.Switch FindSwitch(string id)
        {
            if (switches.ContainsKey(id))
            {
                return switches[id];
            }

            return null;
        }

        public void FindAllLockedDoors()
        {
            areLockedDoorsUnlocked.Clear();

            WorldInteractiveObject[] allWorldInteractiveObjects = FindObjectsOfType<WorldInteractiveObject>();
            NoPowerTip[] allNoPowerTips = FindObjectsOfType<NoPowerTip>();
            foreach (WorldInteractiveObject worldInteractiveObject in allWorldInteractiveObjects)
            {
                // EFT has multiple WorldInteractiveObjects with the same ID on Lighthouse in SPT 3.10. Why, BSG...
                if (IDsForWorldInteractiveObjects.ContainsKey(worldInteractiveObject.Id))
                {
                    LoggingController.LogWarning("Already found WorldInteractiveObject with ID " + worldInteractiveObject.Id + ". Not including the one at " + worldInteractiveObject.transform.position + ".");
                    continue;
                }

                IDsForWorldInteractiveObjects.Add(worldInteractiveObject.Id, worldInteractiveObject);
                //LoggingController.LogInfo("Found door " + door.Id + " at " + door.transform.position + " (State: " + door.DoorState.ToString() + ")");

                if (!worldInteractiveObject.Operatable)
                {
                    //LoggingController.LogInfo("Door " + door.Id + " is inoperable");
                    continue;
                }

                Door door = worldInteractiveObject as Door;
                if ((door != null) && !door.CanBeBreached && (door.KeyId == ""))
                {
                    //LoggingController.LogInfo("Door " + door.Id + " cannot be breached and has no valid key");
                    continue;
                }

                if (worldInteractiveObject.DoorState != EDoorState.Locked)
                {
                    continue;
                }

                areLockedDoorsUnlocked.Add(worldInteractiveObject, false);

                foreach(NoPowerTip noPowerTip in allNoPowerTips)
                {
                    if (!doorHasNoPowerTip(worldInteractiveObject, noPowerTip))
                    {
                        continue;
                    }

                    noPowerTipsForDoors.Add(worldInteractiveObject, noPowerTip);
                    LoggingController.LogDebug("Found NoPowerTip " + noPowerTip.name + " for door " + worldInteractiveObject.Id);
                    break;
                }
            }

            LoggingController.LogDebug("Found " + areLockedDoorsUnlocked.Count + " locked doors");
            //LoggingController.LogInfo("Found locked doors: " + string.Join(", ", areLockedDoorsUnlocked.Select(s => s.Key.Id)));
        }

        private bool doorHasNoPowerTip(WorldInteractiveObject worldInteractiveObject, NoPowerTip noPowerTip)
        {
            if (!noPowerTip.gameObject.TryGetComponent(out BoxCollider collider))
            {
                LoggingController.LogWarning("Could not find collider for NoPowerTip " + noPowerTip.name);
                return false;
            }

            // Check if the door is a keycard door
            KeycardDoor keycardDoor = worldInteractiveObject as KeycardDoor;
            if (keycardDoor != null)
            {
                // Need to expand the collider because the Saferoom keypad on Interchange isn't fully contained by the NoPowerTip for it
                float boundsExpansion = 2.5f;
                Bounds expandedBounds = new Bounds(collider.bounds.center, collider.bounds.size * boundsExpansion);

                // Check if there is a NoPowerTip for any of the keypads for the door (but there should only be one)
                foreach (InteractiveProxy interactiveProxy in keycardDoor.Proxies)
                {
                    if (expandedBounds.Contains(interactiveProxy.transform.position))
                    {
                        return true;
                    }

                    //LoggingController.LogInfo("NoPowerTip " + noPowerTip.name + "(" + expandedBounds.center + " with extents " + expandedBounds.extents + ") does not surround proxy of door " + door.Id + "(" + interactiveProxy.transform.position + ")");
                }
            }
            else
            {
                // Check if the door has a handle, which is what is needed to test if it's within a NoPowerTip collider
                Transform doorTestTransform = worldInteractiveObject.LockHandle?.transform;
                if (doorTestTransform == null)
                {
                    return false;
                }

                if (collider.bounds.Contains(doorTestTransform.position))
                {
                    return true;
                }

                //LoggingController.LogInfo("NoPowerTip " + noPowerTip.name + "(" + collider.bounds.center + ") does not surround door " + door.Id + "(" + doorTestTransform.position + ")");
            }

            return false;
        }

        public WorldInteractiveObject FindWorldInteractiveObjectsByID(string id)
        {
            if (IDsForWorldInteractiveObjects.ContainsKey(id))
            {
                return IDsForWorldInteractiveObjects[id];
            }

            return null;
        }

        public IEnumerable<WorldInteractiveObject> FindAllWorldInteractiveObjectsNearPosition(Vector3 position, float maxDistance)
        {
            return IDsForWorldInteractiveObjects
                .Where(d => Vector3.Distance(position, d.Value.transform.position) < maxDistance)
                .Select(d => d.Value);
        }

        public IEnumerable<WorldInteractiveObject> FindAllWorldInteractiveObjectsNearPosition(Vector3 position, float maxDistance, EDoorState doorState)
        {
            return IDsForWorldInteractiveObjects
                .Where(d => d.Value.DoorState == doorState)
                .Where(d => Vector3.Distance(position, d.Value.transform.position) < maxDistance)
                .Select(d => d.Value);
        }

        public IEnumerable<Door> FindAllDoorsNearPosition(Vector3 position, float maxDistance, EDoorState doorState)
        {
            return FindAllWorldInteractiveObjectsNearPosition(position, maxDistance, doorState)
                .Where(o => o is Door)
                .Select(o => o as Door);
        }

        public IEnumerable<WorldInteractiveObject> FindLockedDoorsNearPosition(Vector3 position, float maxDistance, bool stillLocked = true)
        {
            Dictionary<WorldInteractiveObject, float> lockedDoorsAndDistance = new Dictionary<WorldInteractiveObject, float>();

            foreach (WorldInteractiveObject worldInteractiveObject in areLockedDoorsUnlocked.Keys.ToArray())
            {
                // Remove the door from the dictionary if it has been destroyed (namely due to Backdoor Bandit)
                if (worldInteractiveObject == null)
                {
                    areLockedDoorsUnlocked.Remove(worldInteractiveObject);
                }

                // Check if the door has been unlocked since this method was previously called
                if (!areLockedDoorsUnlocked[worldInteractiveObject] && (worldInteractiveObject.DoorState != EDoorState.Locked))
                {
                    LoggingController.LogInfo("Door " + worldInteractiveObject.Id + " is no longer locked.");
                    areLockedDoorsUnlocked[worldInteractiveObject] = true;
                }

                // Check if the door is within the desired distance
                float distance = Vector3.Distance(position, worldInteractiveObject.transform.position);
                if (distance > maxDistance)
                {
                    continue;
                }

                // Check if the door is locked
                if (stillLocked && !areLockedDoorsUnlocked[worldInteractiveObject])
                {
                    lockedDoorsAndDistance.Add(worldInteractiveObject, distance);
                }

                // Check if the door is unlocked
                if (!stillLocked && areLockedDoorsUnlocked[worldInteractiveObject])
                {
                    lockedDoorsAndDistance.Add(worldInteractiveObject, distance);
                }
            }

            // Sort the matching doors based on distance to the position
            return lockedDoorsAndDistance.OrderBy(d => d.Value).Select(d => d.Key);
        }

        public void ReportUnlockedDoor(WorldInteractiveObject worldInteractiveObject)
        {
            if (areLockedDoorsUnlocked.ContainsKey(worldInteractiveObject))
            {
                areLockedDoorsUnlocked[worldInteractiveObject] = true;
            }
        }

        public SpawnPointParams[] GetAllValidSpawnPointParams()
        {
            if (CurrentLocation == null)
            {
                throw new InvalidOperationException("Spawn-point data can only be retrieved after a map has been loaded.");
            }

            if (CurrentLocation.Id == "TarkovStreets")
            {
                // Band-aid fix for BSG not completing the generation of bot-cell data for all parts of the map
                SpawnPointParams[] validSpawnPointParams = CurrentLocation.SpawnPointParams
                    .Where(s => s.Position.z < 440)
                    .ToArray();

                return validSpawnPointParams;
            }

            if (CurrentLocation.Id.Contains("factory"))
            {
                
                SpawnPointParams[] validSpawnPointParams = CurrentLocation.SpawnPointParams
                    .Where(s => !isOutsideNearTransitOnFactory(s.Position) && !isInsideBrokenSiloOnFactory(s.Position))
                    .ToArray();

                return validSpawnPointParams;
            }

            return CurrentLocation.SpawnPointParams;
        }

        private static bool isOutsideNearTransitOnFactory(Vector3 position)
        {
            return (position.x > 8) && (position.x < 33) && (position.z > 45) && (position.z < 65);
        }

        private static bool isInsideBrokenSiloOnFactory(Vector3 position)
        {
            return (position.x > -9) && (position.x < 0) && (position.z > 12) && (position.z < 20);
        }

        // This isn't actually used anywhere in this mod, but I left it in here because it's a pretty nifty algorithm
        public bool TryGetObjectNearPosition<T>(Vector3 position, float maxDistance, bool onlyVisible, out T obj) where T: Behaviour
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

        public Vector3? GetMainPlayerPosition()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        public SpawnPointParams? GetMainPlayerSpawnPoint()
        {
            Vector3? playerPosition = GetMainPlayerPosition();
            if (!playerPosition.HasValue)
            {
                return null;
            }

            return GetNearestSpawnPoint(playerPosition.Value);
        }

        public Vector3? FindNearestNavMeshPosition(Vector3 position, float searchDistance)
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

        public bool AreAnyPositionsCloseToOtherPlayers(IEnumerable<Vector3> positions, float distanceFromPlayers, out float distance)
        {
            foreach (Vector3 postion in positions)
            {
                if (IsPositionCloseToOtherPlayers(postion, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public bool IsPositionCloseToOtherPlayers(Vector3 position, float distanceFromPlayers, out float distance)
        {
            BotsController botControllerClass = Singleton<IBotGame>.Instance.BotsController;

            BotOwner closestBot = botControllerClass.ClosestBotToPoint(position);
            if (closestBot != null)
            {
                distance = Vector3.Distance(position, closestBot.Position);
                if ((closestBot != null) && (distance < distanceFromPlayers))
                {
                    return true;
                }
            }

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI))
            {
                distance = Vector3.Distance(position, player.Position);
                if (distance < distanceFromPlayers)
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromAllPlayers(ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides)
        {
            return TryGetFurthestSpawnPointFromPlayers(Singleton<GameWorld>.Instance.AllAlivePlayersList, allowedCategories, allowedSides, new SpawnPointParams[0]);
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromAllPlayers(ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides, SpawnPointParams[] excludedSpawnPoints)
        {
            return TryGetFurthestSpawnPointFromPlayers(Singleton<GameWorld>.Instance.AllAlivePlayersList, allowedCategories, allowedSides, new SpawnPointParams[0]);
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromPlayers(IEnumerable<Player> players, ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides, float distanceFromAllPlayers = 5)
        {
            return TryGetFurthestSpawnPointFromPlayers(players, allowedCategories, allowedSides, new SpawnPointParams[0], distanceFromAllPlayers);
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromPlayers(Vector3[] positions, ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides, float distanceFromAllPlayers = 5)
        {
            return TryGetFurthestSpawnPointFromPositions(positions, allowedCategories, allowedSides, new SpawnPointParams[0], distanceFromAllPlayers);
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromPlayers(IEnumerable<Player> players, ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides, SpawnPointParams[] excludedSpawnPoints, float distanceFromAllPlayers = 5)
        {
            Vector3[] playerPositions = players.Select(p => p.Position).ToArray();
            return TryGetFurthestSpawnPointFromPositions(playerPositions, allowedCategories, allowedSides, excludedSpawnPoints, distanceFromAllPlayers);
        }

        public SpawnPointParams? TryGetFurthestSpawnPointFromPositions(Vector3[] positions, ESpawnCategoryMask allowedCategories, EPlayerSideMask allowedSides, SpawnPointParams[] excludedSpawnPoints, float distanceFromAllPlayers = 5)
        {
            Vector3[] allPlayerPositions = Singleton<GameWorld>.Instance.AllAlivePlayersList.Select(p => p.Position).ToArray();
            SpawnPointParams[] allSpawnPoints = GetAllValidSpawnPointParams();

            // Enumerate all valid spawn points
            IEnumerable<SpawnPointParams> validSpawnPoints = allSpawnPoints
                .Where(s => !excludedSpawnPoints.Contains(s))
                .Where(s => s.Categories.Any(allowedCategories))
                .Where(s => s.Sides.Any(allowedSides));
            
            // Remove spawn points that are too close to other bots or you
            SpawnPointParams[] eligibleSpawnPoints = validSpawnPoints
                .Where(s => allPlayerPositions.All(p => Vector3.Distance(s.Position, p) > distanceFromAllPlayers))
                .ToArray();

            if (eligibleSpawnPoints.Length == 0)
            {
                if (validSpawnPoints.Any())
                {
                    float maxDistance = validSpawnPoints
                        .Select(s => allPlayerPositions.Min(p => Vector3.Distance(s.Position, p)))
                        .Max();

                    LoggingController.LogWarning("Maximum distance from other players using " + validSpawnPoints.Count() + " spawn points: " + maxDistance);
                }
                else
                {
                    LoggingController.LogWarning("No valid spawn points");
                }

                return null;
            }

            // Get the locations of all alive bots/players on the map.
            if (positions.Length == 0)
            {
                LoggingController.LogWarning("No player positions");
                return null;
            }

            //LoggingController.LogInfo("Alive players: " + string.Join(", ", Singleton<GameWorld>.Instance.AllAlivePlayersList.Select(s => s.Profile.Nickname)));

            return GetFurthestSpawnPoint(positions, eligibleSpawnPoints);
        }

        public SpawnPointParams GetFurthestSpawnPoint(Vector3[] referencePositions, SpawnPointParams[] allSpawnPoints)
        {
            if (referencePositions.Length == 0)
            {
                throw new ArgumentException("The reference position array is empty.", nameof(referencePositions));
            }

            if (allSpawnPoints.Length == 0)
            {
                throw new ArgumentException("The spawn-point array is empty.", nameof(allSpawnPoints));
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

            LoggingController.LogDebug("Found furthest spawn point " + selectedPoint.Key.Position.ToUnityVector3().ToString() + " that is " + selectedPoint.Value + "m from other players");

            return selectedPoint.Key;
        }

        public SpawnPointParams GetFurthestSpawnPoint(SpawnPointParams[] referenceSpawnPoints, SpawnPointParams[] allSpawnPoints)
        {
            return GetFurthestSpawnPoint(referenceSpawnPoints.Select(p => p.Position.ToUnityVector3()).ToArray(), allSpawnPoints);
        }

        public IEnumerable<SpawnPointParams> GetNearestSpawnPoints(Vector3 position, int count)
        {
            return GetNearestSpawnPoints(position, count, new SpawnPointParams[0]);
        }

        public IEnumerable<SpawnPointParams> GetNearestSpawnPoints(Vector3 position, int count, SpawnPointParams[] excludedSpawnPoints)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "At least 1 spawn point must be requested");
            }

            // If there are multiple bots that will spawn, select nearby spawn points for each of them
            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>();
            while (spawnPoints.Count < count)
            {
                SpawnPointParams nextPosition = GetNearestSpawnPoint(position, spawnPoints.ToArray().AddRangeToArray(excludedSpawnPoints));

                Vector3? navMeshPosition = FindNearestNavMeshPosition(nextPosition.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    excludedSpawnPoints = excludedSpawnPoints.AddItem(nextPosition).ToArray();
                    continue;
                }

                spawnPoints.Add(nextPosition);
            }

            return spawnPoints;
        }

        public IEnumerable<SpawnPointParams> GetNearbySpawnPoints(Vector3 position, float distance)
        {
            return GetNearbySpawnPoints(position, distance, GetAllValidSpawnPointParams());
        }

        public IEnumerable<SpawnPointParams> GetNearbySpawnPoints(Vector3 position, float distance, SpawnPointParams[] allSpawnPoints)
        {
            return allSpawnPoints.Where(s => Vector3.Distance(position, s.Position) < distance);
        }

        public SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints, SpawnPointParams[] allSpawnPoints)
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

        public SpawnPointParams GetNearestSpawnPoint(Vector3 postition)
        {
            return GetNearestSpawnPoint(postition, new SpawnPointParams[0], GetAllValidSpawnPointParams());
        }

        public SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints)
        {
            return GetNearestSpawnPoint(postition, excludedSpawnPoints, GetAllValidSpawnPointParams());
        }

        public Vector3? GetDoorInteractionPosition(WorldInteractiveObject door, Vector3 startingPosition)
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
                Vector3? navMeshPosition = FindNearestNavMeshPosition(possibleInteractionPosition, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceDoors);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogInfo("Cannot access position " + possibleInteractionPosition.ToString() + " for door " + door.Id);

                    if (ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.ShowDoorInteractionTestPoints)
                    {
                        DebugHelpers.outlinePosition(possibleInteractionPosition, Color.white, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceDoors);
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
                    DebugHelpers.outlinePosition(navMeshPosition.Value, Color.yellow);
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
                    DebugHelpers.outlinePosition(orderedPostions.First(), Color.green);

                    foreach (Vector3 alternatePosition in orderedPostions.Skip(1))
                    {
                        DebugHelpers.outlinePosition(alternatePosition, Color.magenta);
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

        public WorldInteractiveObject FindFirstAccessibleDoor(IEnumerable<WorldInteractiveObject> worldInteractiveObjects, Vector3 startingPosition)
        {
            foreach (WorldInteractiveObject worldInteractiveObject in worldInteractiveObjects)
            {
                // Ensure the door hasn't been destroyed (namely by BackdoorBandit)
                if (worldInteractiveObject == null)
                {
                    continue;
                }

                // Prevent the inner KIBA door from being unlocked before the outer KIBA door
                if (worldInteractiveObject.Id == "Shopping_Mall_DesignStuff_00049")
                {
                    IEnumerable<bool> kibaOuterDoor = areLockedDoorsUnlocked
                        .Where(d => d.Key.Id == "Shopping_Mall_DesignStuff_00050")
                        .Select(d => d.Value);

                    if (kibaOuterDoor.Any(v => v == false))
                    {
                        LoggingController.LogInfo("Cannot unlock inner KIBA door until outer KIBA door is unlocked");
                        continue;
                    }
                }

                // Prevent doors that require power from being unlocked before the power is turned on
                if (noPowerTipsForDoors.ContainsKey(worldInteractiveObject) && noPowerTipsForDoors[worldInteractiveObject].isActiveAndEnabled)
                {
                    LoggingController.LogInfo("NoPowerTip for door " + worldInteractiveObject.Id + " is still active.");
                    continue;
                }

                // Ensure a player can interact with the door
                ActionsReturnClass availableActionsResult = GetActionsClass.GetAvailableActions(gamePlayerOwner, worldInteractiveObject);
                if ((availableActionsResult == null) || !availableActionsResult.Actions.Any())
                {
                    continue;
                }

                //LoggingController.LogInfo("Actions for door " + door.Id + ": " + string.Join(", ", availableActionsResult.Actions.Select(a => a.Name)));

                Vector3? interactionPosition = GetDoorInteractionPosition(worldInteractiveObject, startingPosition);
                if (interactionPosition.HasValue)
                {
                    return worldInteractiveObject;
                }
            }

            return null;
        }

        public float GetMaxDistanceBetweenExfils()
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

        private void calculateMaxDistanceBetweenSpawnPoints()
        {
            float maxDistance = 0;
            foreach (SpawnPointParams spawnPointParams in CurrentLocation.SpawnPointParams)
            {
                foreach (SpawnPointParams spawnPointParams2 in CurrentLocation.SpawnPointParams)
                {
                    float distance = Vector3.Distance(spawnPointParams.Position, spawnPointParams2.Position);
                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                    }
                }
            }

            MaxDistanceBetweenSpawnPoints = maxDistance;
        }

        private void handleCustomQuestKeypress()
        {
            if (!QuestingBotsPluginConfig.CreateQuestLocations.Value)
            {
                return;
            }

            if (!QuestingBotsPluginConfig.StoreQuestLocationKey.Value.IsDown())
            {
                return;
            }

            if (QuestingBotsPluginConfig.QuestLocationName.Value.Length == 0)
            {
                LoggingController.LogErrorToServerConsole("The name of custom quest locations cannot be an empty string. Please create a name in the F12 advanced menu.");
                return;
            }

            if (!RaidHelpers.HasRaidStarted())
            {
                return;
            }

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (!mainPlayer.isActiveAndEnabled || !mainPlayer.HealthController.IsAlive)
            {
                return;
            }

            Models.Questing.StoredQuestLocation location = new Models.Questing.StoredQuestLocation(QuestingBotsPluginConfig.QuestLocationName.Value, mainPlayer.Position);

            string filename = ConfigController.GetLoggingPath()
                + CurrentLocation.Id.Replace(" ", "")
                + "_"
                + awakeTime.ToFileTimeUtc()
                + "_customQuestLocations.json";

            LoggingController.AppendQuestLocation(filename, location);
        }
    }
}
