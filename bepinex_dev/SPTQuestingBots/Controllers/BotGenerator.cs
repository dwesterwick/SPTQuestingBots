using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using QuestingBots.CoroutineExtensions;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Controllers
{
    public class BotGenerator : MonoBehaviour
    {
        public static bool IsSpawningPMCs { get; private set; } = false;
        public static bool IsGeneratingPMCs { get; private set; } = false;
        public static int SpawnedPMCCount { get; private set; } = 0;

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static RaidSettings raidSettings = null;
        private static LocationSettingsClass.Location location = null;
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static List<Models.BotSpawnInfo> initialPMCBots = new List<Models.BotSpawnInfo>();
        private static SpawnPointParams[] initialPMCBotSpawnPoints = new SpawnPointParams[0];
        private static Dictionary<string, Vector3> spawnPositions = new Dictionary<string, Vector3>();
        private static int maxPMCBots = 0;

        public static ReadOnlyCollection<Models.BotSpawnInfo> InitiallySpawnedPMCBots
        {
            get { return new ReadOnlyCollection<Models.BotSpawnInfo>(initialPMCBots); }
        }

        public static void Clear()
        {
            if (IsSpawningPMCs)
            {
                enumeratorWithTimeLimit.Abort();
                TaskWithTimeLimit.WaitForCondition(() => !IsSpawningPMCs);
            }

            if (IsGeneratingPMCs)
            {
                TaskWithTimeLimit.WaitForCondition(() => !IsGeneratingPMCs);
            }

            initialPMCBots.Clear();
            initialPMCBotSpawnPoints = new SpawnPointParams[0];
            spawnPositions.Clear();
            location = null;
            raidSettings = null;
        }

        public static bool IsBotFromInitialPMCSpawns(BotOwner bot)
        {
            return InitiallySpawnedPMCBots.Any(b => b.Owner ==  bot);
        }

        public static Vector3? GetPlayerPosition()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return Singleton<GameWorld>.Instance.MainPlayer.Position;
        }

        private void Update()
        {
            if ((!Singleton<GameWorld>.Instantiated) || (Camera.main == null))
            {
                Clear();

                SpawnedPMCCount = 0;
                return;
            }

            if (IsSpawningPMCs || IsGeneratingPMCs || (SpawnedPMCCount > 0))
            {
                return;
            }

            if (location == null)
            {
                raidSettings = GetCurrentRaidSettings();
                location = raidSettings.SelectedLocation;
            }

            if (!ConfigController.Config.InitialPMCSpawns.Enabled)
            {
                return;
            }

            // Get the current number of seconds remaining and elapsed in the raid
            float escapeTimeSec = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (location.EscapeTimeLimit * 60f) - escapeTimeSec;

            // Do not force spawns if the player spawned late
            if (raidTimeElapsed > ConfigController.Config.InitialPMCSpawns.MaxRaidET)
            {
                return;
            }

            if (initialPMCBots.Count == 0)
            {
                System.Random random = new System.Random();
                maxPMCBots = random.Next(location.MinPlayers, location.MaxPlayers)  - 1;

                ConfigController.ForcePMCSpawns();

                // Create bot data from the server
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                generateBots(WildSpawnType.assault, EPlayerSide.Savage, raidSettings.WavesSettings.BotDifficulty.ToBotDifficulty(), maxPMCBots);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                // Generate inital PMC spawn points
                initialPMCBotSpawnPoints = getPMCSpawnPoints(location.SpawnPointParams, maxPMCBots);

                return;
            }

            // Ensure the raid is progressing before running anything
            if (raidTimeElapsed < 1)
            {
                return;
            }

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            for (int b = 0; b < initialPMCBots.Count; b++)
            {
                LoggingController.LogInfo("Assigning initial PMC bot #" + b + " the spawn point " + initialPMCBotSpawnPoints[b].Position.ToUnityVector3().ToString());
                initialPMCBots[b].SpawnPoint = initialPMCBotSpawnPoints[b];
            }

            StartCoroutine(SpawnInitialPMCs(initialPMCBots, botSpawnerClass, location.SpawnPointParams));
            ConfigController.ForceScavSpawns();
        }

        public static float GetRaidET()
        {
            // Get the current number of seconds remaining and elapsed in the raid
            float escapeTimeSec = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (location.EscapeTimeLimit * 60f) - escapeTimeSec;

            return raidTimeElapsed;
        }

        public static Models.Quest CreateSpawnPointQuest(ESpawnCategoryMask spawnTypes = ESpawnCategoryMask.All)
        {
            IEnumerable<SpawnPointParams> eligibleSpawnPoints = location.SpawnPointParams.Where(s => s.Categories.Any(spawnTypes));
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
                quest.AddObjective(objective);
            }
            
            return quest;
        }

        public static Models.Quest CreateSpawnRushQuest()
        {
            if (GetRaidET() > ConfigController.Config.BotQuests.SpawnRush.MaxRaidET)
            {
                return null;
            }

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

            Models.Quest quest = new Models.Quest(1, "Spawn Rush");
            quest.ChanceForSelecting = ConfigController.Config.BotQuests.SpawnRush.Chance;
            Models.QuestSpawnPointObjective objective = new Models.QuestSpawnPointObjective(playerSpawnPoint.Value, navMeshPosition.Value);
            objective.MaxDistanceFromBot = ConfigController.Config.BotQuests.SpawnRush.MaxDistance;
            quest.AddObjective(objective);
            return quest;
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

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] allSpawnPoints)
        {
            SpawnPointParams nearestSpawnPoint = allSpawnPoints[0];
            float nearestDistance = Vector3.Distance(postition, nearestSpawnPoint.Position.ToUnityVector3());

            for (int s = 1; s < allSpawnPoints.Length; s++)
            {
                float distance = Vector3.Distance(postition, allSpawnPoints[s].Position.ToUnityVector3());

                if (distance < nearestDistance)
                {
                    nearestSpawnPoint = allSpawnPoints[s];
                    nearestDistance = distance;
                }
            }

            return nearestSpawnPoint;
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

        public static LocationSettingsClass.Location GetCurrentLocation()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return GetCurrentRaidSettings().SelectedLocation;
        }

        public static RaidSettings GetCurrentRaidSettings()
        {
            TarkovApplication app = FindObjectOfType<TarkovApplication>();
            if (app == null)
            {
                LoggingController.LogError("Cannot retrieve Tarkov application instance");
                return null;
            }

            FieldInfo raidSettingsField = typeof(TarkovApplication).GetField("_raidSettings", BindingFlags.NonPublic | BindingFlags.Instance);
            RaidSettings raidSettings = raidSettingsField.GetValue(app) as RaidSettings;
            return raidSettings;
        }

        private static SpawnPointParams? getPlayerSpawnPoint()
        {
            Vector3? playerPosition = GetPlayerPosition();
            if (!playerPosition.HasValue)
            {
                return null;
            }

            return GetNearestSpawnPoint(playerPosition.Value, location.SpawnPointParams);
        }

        private async Task generateBots(WildSpawnType wildSpawnType, EPlayerSide side, BotDifficulty botdifficulty, int count)
        {
            IsGeneratingPMCs = true;

            LoggingController.LogInfo("Generating PMC bots...");

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawnerClass), "ginterface17_0").GetValue(botSpawnerClass) as IBotCreator;
            IBotData botData = new GClass629(side, wildSpawnType, botdifficulty, 0f, null);

            for (int i = 0; i < count; i++)
            {
                // This causes a deadlock for some reason
                /*if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }*/

                LoggingController.LogInfo("Generating PMC bot #" + i + "...");
                GClass628 newBotData = await GClass628.Create(botData, ibotCreator, 1, botSpawnerClass);
                initialPMCBots.Add(new Models.BotSpawnInfo(newBotData));
            }

            LoggingController.LogInfo("Generating PMC bots...done.");

            IsGeneratingPMCs = false;
        }

        private IEnumerator SpawnInitialPMCs(IEnumerable<Models.BotSpawnInfo> initialPMCs, BotSpawnerClass botSpawnerClass, SpawnPointParams[] allSpawnPoints)
        {
            IsSpawningPMCs = true;

            Dictionary<string, float> originalSpawnDelays = new Dictionary<string, float>();
            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                originalSpawnDelays.Add(allSpawnPoints[s].Id, allSpawnPoints[s].DelayToCanSpawnSec);
                allSpawnPoints[s].DelayToCanSpawnSec = 0;
            }

            yield return enumeratorWithTimeLimit.Run(initialPMCs, spawnInitialPMCAtSpawnPoint, botSpawnerClass);

            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                allSpawnPoints[s].DelayToCanSpawnSec = originalSpawnDelays[allSpawnPoints[s].Id];
            }

            IsSpawningPMCs = false;
        }

        private SpawnPointParams[] getPMCSpawnPoints(SpawnPointParams[] allSpawnPoints, int count)
        {
            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>();

            List<SpawnPointParams> validSpawnPoints = new List<SpawnPointParams>();
            foreach(SpawnPointParams spawnPoint in allSpawnPoints)
            {
                if (!spawnPoint.Categories.Contain(ESpawnCategory.Player))
                {
                    continue;
                }

                Vector3 spawnPosition = spawnPoint.Position.ToUnityVector3();
                Vector3? navMeshPosition = BotGenerator.FindNearestNavMeshPosition(spawnPosition, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    LoggingController.LogInfo("Cannot spawn PMC at " + spawnPoint.Id + ". No valid NavMesh position nearby.");
                    continue;
                }

                if (!spawnPositions.ContainsKey(spawnPoint.Id))
                {
                    spawnPositions.Add(spawnPoint.Id, navMeshPosition.Value);
                }

                if (Vector3.Distance(navMeshPosition.Value, Singleton<GameWorld>.Instance.MainPlayer.Position) < 20)
                {
                    LoggingController.LogInfo("Cannot spawn PMC at " + spawnPoint.Id + ". Too close to player.");
                    continue;
                }

                validSpawnPoints.Add(spawnPoint);
            }

            SpawnPointParams playerSpawnPoint = GetNearestSpawnPoint(Singleton<GameWorld>.Instance.MainPlayer.Position, allSpawnPoints.ToArray());
            LoggingController.LogInfo("Nearest spawn point to player: " + playerSpawnPoint.Position.ToUnityVector3().ToString());
            spawnPoints.Add(playerSpawnPoint);

            for (int s = 0; s < count; s++)
            {
                SpawnPointParams newSpawnPoint = GetFurthestSpawnPoint(spawnPoints.ToArray(), validSpawnPoints.ToArray());
                LoggingController.LogInfo("Found furthest spawn point: " + newSpawnPoint.Position.ToUnityVector3().ToString());
                spawnPoints.Add(newSpawnPoint);
            }

            spawnPoints.Remove(playerSpawnPoint);

            return spawnPoints.ToArray();
        }

        private void spawnInitialPMCAtSpawnPoint(Models.BotSpawnInfo initialPMCBot, BotSpawnerClass botSpawnerClass)
        {
            if (SpawnedPMCCount > maxPMCBots)
            {
                LoggingController.LogWarning("Max PMC count of " + maxPMCBots + " already reached.");
                return;
            }

            if (!initialPMCBot.SpawnPoint.HasValue && !initialPMCBot.SpawnPosition.HasValue)
            {
                LoggingController.LogError("No spawn position assigned to initial PMC bot #" + (SpawnedPMCCount + 1) + ".");
                return;
            }

            if (!initialPMCBot.SpawnPosition.HasValue)
            {
                initialPMCBot.AssignSpawnPositionFromSpawnPoint();
            }

            Action<BotOwner> callback = new Action<BotOwner>((botOwner) =>
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spawned as an initial PMC.");
                initialPMCBot.Owner = botOwner;
            });

            LoggingController.LogInfo("Spawning PMC #" + (SpawnedPMCCount + 1) + " at " + initialPMCBot.SpawnPosition.Value.ToString() + "...");
            spawnBot(initialPMCBot.Data, initialPMCBot.SpawnPosition.Value, callback, botSpawnerClass);

            SpawnedPMCCount++;
        }

        private void spawnBot(GClass628 bot, Vector3 position, Action<BotOwner> callback, BotSpawnerClass botSpawnerClass)
        {
            BotZone closestBotZone = botSpawnerClass.GetClosestZone(position, out float dist);
            bot.AddPosition(position);

            MethodInfo botSpawnMethod = typeof(BotSpawnerClass).GetMethod("method_11", BindingFlags.Instance | BindingFlags.NonPublic);
            botSpawnMethod.Invoke(botSpawnerClass, new object[] { closestBotZone, bot, callback, botSpawnerClass.GetCancelToken() });
        }
    }
}
