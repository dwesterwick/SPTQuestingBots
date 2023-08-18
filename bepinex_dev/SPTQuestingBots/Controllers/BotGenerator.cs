using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.Interactive;
using HarmonyLib;
using QuestingBots.CoroutineExtensions;
using QuestingBots.Models;
using QuestingBots.Patches;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Controllers
{
    public class BotGenerator : MonoBehaviour
    {
        public static bool CanSpawnPMCs { get; private set; } = true;
        public static bool IsSpawningPMCs { get; private set; } = false;
        public static bool IsGeneratingPMCs { get; private set; } = false;
        public static int SpawnedPMCCount { get; private set; } = 0;

        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static RaidSettings raidSettings = null;
        private static LocationSettingsClass.Location location = null;
        private static Dictionary<Vector3, Vector3> nearestNavMeshPoint = new Dictionary<Vector3, Vector3>();
        private static List<Models.BotSpawnInfo> initialPMCGroups = new List<Models.BotSpawnInfo>();
        private static SpawnPointParams[] initialPMCBotSpawnPoints = new SpawnPointParams[0];
        private static Dictionary<string, Vector3> spawnPositions = new Dictionary<string, Vector3>();
        private static int maxPMCBots = 0;

        public static ReadOnlyCollection<Models.BotSpawnInfo> InitiallySpawnedPMCBots
        {
            get { return new ReadOnlyCollection<Models.BotSpawnInfo>(initialPMCGroups); }
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

            initialPMCGroups.Clear();
            initialPMCBotSpawnPoints = new SpawnPointParams[0];
            spawnPositions.Clear();
            location = null;
            raidSettings = null;
        }

        public static bool IsBotFromInitialPMCSpawns(BotOwner bot)
        {
            return InitiallySpawnedPMCBots.Any(b => b.Owners.Contains(bot));
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
            if (!Singleton<IBotGame>.Instantiated)
            {
                Clear();

                CanSpawnPMCs = true;
                SpawnedPMCCount = 0;
                return;
            }

            if (!CanSpawnPMCs || IsSpawningPMCs || IsGeneratingPMCs || (SpawnedPMCCount > 0))
            {
                return;
            }

            if (location == null)
            {
                raidSettings = GetCurrentRaidSettings();
                location = raidSettings.SelectedLocation;
            }

            if (!ConfigController.Config.InitialPMCSpawns.Enabled || (raidSettings.BotSettings.BotAmount == EFT.Bots.EBotAmount.NoBots))
            {
                return;
            }

            // Get the current number of seconds remaining and elapsed in the raid
            float escapeTimeSec = GClass1473.EscapeTimeSeconds(Singleton<AbstractGame>.Instance.GameTimer);
            float raidTimeElapsed = (location.EscapeTimeLimit * 60f) - escapeTimeSec;

            // Do not force spawns if the player spawned late
            if (raidTimeElapsed > ConfigController.Config.InitialPMCSpawns.MaxRaidET)
            {
                if (initialPMCGroups.Count == 0)
                {
                    ConfigController.AdjustPMCConversionChances(1);
                }

                CanSpawnPMCs = false;
                return;
            }

            if (initialPMCGroups.Count == 0)
            {
                System.Random random = new System.Random();
                maxPMCBots = random.Next(location.MinPlayers, location.MaxPlayers) - 1;

                ConfigController.AdjustPMCConversionChances(ConfigController.Config.InitialPMCSpawns.ConversionFactorAfterInitialSpawns);
                
                // Create bot data from the server
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                generateBots(raidSettings.WavesSettings.BotDifficulty.ToBotDifficulty(), maxPMCBots);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                // Generate inital PMC spawn points
                initialPMCBotSpawnPoints = getPMCSpawnPoints(location.SpawnPointParams, maxPMCBots);

                return;
            }

            // Ensure the raid is progressing before running anything
            if ((raidTimeElapsed < 1) || ((BotOwnerCreatePatch.SpawnedBotCount < InitBossSpawnLocationPatch.ZeroWaveBotCount) && !location.Name.ToLower().Contains("factory")))
            {
                return;
            }

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            for (int b = 0; b < initialPMCGroups.Count; b++)
            {
                LoggingController.LogInfo("Assigning initial PMC group #" + b + " the spawn point " + initialPMCBotSpawnPoints[b].Position.ToUnityVector3().ToString());
                initialPMCGroups[b].SpawnPoint = initialPMCBotSpawnPoints[b];
            }

            StartCoroutine(SpawnInitialPMCs(initialPMCGroups, botSpawnerClass, location.SpawnPointParams));
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
                objective.MaxBots = ConfigController.Config.BotQuests.SpawnPointWander.MaxBotsPerQuest;
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
            objective.MaxBots = ConfigController.Config.BotQuests.SpawnRush.MaxBotsPerQuest;
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
            return GetNearestSpawnPoint(postition, new SpawnPointParams[0], location.SpawnPointParams);
        }

        public static SpawnPointParams GetNearestSpawnPoint(Vector3 postition, SpawnPointParams[] excludedSpawnPoints)
        {
            return GetNearestSpawnPoint(postition, excludedSpawnPoints, location.SpawnPointParams);
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

        private EPlayerSide GetSideForWildSpawnType(WildSpawnType spawnType)
        {
            WildSpawnType sptUsec = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue;
            WildSpawnType sptBear = (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue;

            if (spawnType == WildSpawnType.pmcBot || spawnType == sptUsec)
            {
                return EPlayerSide.Usec;
            }
            else if (spawnType == sptBear)
            {
                return EPlayerSide.Bear;
            }
            else
            {
                return EPlayerSide.Savage;
            }
        }

        private async Task generateBots(BotDifficulty botdifficulty, int totalCount)
        {
            IsGeneratingPMCs = true;

            LoggingController.LogInfo("Generating PMC bots...");

            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawnerClass), "ginterface17_0").GetValue(botSpawnerClass) as IBotCreator;
            
            System.Random random = new System.Random();
            int botsGenerated = 0;
            int botGroup = 1;
            while (botsGenerated < totalCount)
            {
                int botsInGroup = random.Next(1, 1);
                botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                WildSpawnType[] spawnTypes = new WildSpawnType[2]
                {
                    (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptUsecValue,
                    (WildSpawnType)Aki.PrePatch.AkiBotsPrePatcher.sptBearValue
                };

                WildSpawnType spawnType = spawnTypes.Random();
                EPlayerSide spawnSide = GetSideForWildSpawnType(spawnType);

                GClass618 spawnParams = new GClass618();
                spawnParams.TriggerType = SpawnTriggerType.none;
                //spawnParams.Id_spawn = "InitialPMCGroup" + botGroup;
                if (botsInGroup > 1)
                {
                    spawnParams.ShallBeGroup = new GClass619(true, false, botsInGroup);
                }

                IBotData botData = new GClass629(spawnSide, spawnType, botdifficulty, 0f, spawnParams);

                // This causes a deadlock for some reason
                /*if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    break;
                }*/

                LoggingController.LogInfo("Generating PMC group spawn #" + botGroup + "(Number of bots: " + botsInGroup + ")...");
                GClass628 newBotData = await GClass628.Create(botData, ibotCreator, botsInGroup, botSpawnerClass);
                initialPMCGroups.Add(new Models.BotSpawnInfo(newBotData));

                botsGenerated += botsInGroup;
                botGroup++;
            }

            LoggingController.LogInfo("Generating PMC bots...done.");

            IsGeneratingPMCs = false;
        }

        private IEnumerator SpawnInitialPMCs(IEnumerable<Models.BotSpawnInfo> initialPMCGroups, BotSpawnerClass botSpawnerClass, SpawnPointParams[] allSpawnPoints)
        {
            IsSpawningPMCs = true;

            Dictionary<string, float> originalSpawnDelays = new Dictionary<string, float>();
            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                originalSpawnDelays.Add(allSpawnPoints[s].Id, allSpawnPoints[s].DelayToCanSpawnSec);
                allSpawnPoints[s].DelayToCanSpawnSec = 0;
            }

            enumeratorWithTimeLimit.Reset();
            yield return enumeratorWithTimeLimit.Run(initialPMCGroups, spawnInitialPMCsAtSpawnPoint, botSpawnerClass);

            for (int s = 0; s < allSpawnPoints.Length; s++)
            {
                allSpawnPoints[s].DelayToCanSpawnSec = originalSpawnDelays[allSpawnPoints[s].Id];
            }

            yield return null;
            yield return null;

            foreach (KeyValuePair<BotZone, GClass510> keyValuePair in Singleton<IBotGame>.Instance.BotsController.Groups())
            {
                foreach (BotGroupClass botGroupClass in keyValuePair.Value.GetGroups(true))
                {
                    LoggingController.LogInfo("Bot Group Allies: " + string.Join(", ", botGroupClass.Allies.Select(b => b.Profile.Nickname)));
                }
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

        private void spawnInitialPMCsAtSpawnPoint(Models.BotSpawnInfo initialPMCBot, BotSpawnerClass botSpawnerClass)
        {
            if (SpawnedPMCCount > maxPMCBots)
            {
                LoggingController.LogWarning("Max PMC count of " + maxPMCBots + " already reached.");
                return;
            }

            if (!initialPMCBot.SpawnPoint.HasValue && (initialPMCBot.SpawnPositions.Length == 0))
            {
                LoggingController.LogError("No spawn position assigned to initial PMC group #" + (SpawnedPMCCount + 1) + ".");
                return;
            }

            if (initialPMCBot.SpawnPositions.Length == 0)
            {
                initialPMCBot.AssignSpawnPositionsFromSpawnPoint(initialPMCBot.Data.Count);
            }

            Action<BotOwner> callback = new Action<BotOwner>((botOwner) =>
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spawned as an initial PMC.");
                initialPMCBot.Owners = new BotOwner[1] { botOwner };
            });

            LoggingController.LogInfo("Spawning PMC group #" + (SpawnedPMCCount + 1) + " at " + initialPMCBot.SpawnPositions[0] + "...");
            spawnBots(initialPMCBot.Data, initialPMCBot.SpawnPositions, callback, botSpawnerClass);

            SpawnedPMCCount++;
        }

        private void spawnBots(GClass628 bots, Vector3[] positions, Action<BotOwner> callback, BotSpawnerClass botSpawnerClass)
        {
            BotZone closestBotZone = botSpawnerClass.GetClosestZone(positions[0], out float dist);
            foreach (Vector3 position in positions)
            {
                bots.AddPosition(position);
            }

            MethodInfo botSpawnMethod = typeof(BotSpawnerClass).GetMethod("method_11", BindingFlags.Instance | BindingFlags.NonPublic);
            botSpawnMethod.Invoke(botSpawnerClass, new object[] { closestBotZone, bots, callback, botSpawnerClass.GetCancelToken() });
        }
    }
}
