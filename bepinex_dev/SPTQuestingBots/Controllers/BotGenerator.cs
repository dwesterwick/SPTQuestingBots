using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace QuestingBots.Controllers
{
    public class BotGenerator : MonoBehaviour
    {
        public static bool CanSpawnPMCs { get; private set; } = true;
        public static bool IsSpawningPMCs { get; private set; } = false;
        public static bool IsGeneratingPMCs { get; private set; } = false;
        
        private static EnumeratorWithTimeLimit enumeratorWithTimeLimit = new EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static Stopwatch retrySpawnTimer = Stopwatch.StartNew();
        private static List<Models.BotSpawnInfo> initialPMCGroups = new List<Models.BotSpawnInfo>();
        private static List<string> blacklistedSpawnPointIDs = new List<string>();
        private static int maxPMCBots = 0;

        public static ReadOnlyCollection<Models.BotSpawnInfo> InitiallySpawnedPMCBots
        {
            get { return new ReadOnlyCollection<Models.BotSpawnInfo>(initialPMCGroups); }
        }

        public static int SpawnedInitialPMCCount
        {
            get {  return initialPMCGroups.Count(g => g.HasSpawned); }
        }

        public static int RemainingInitialPMCSpawnCount
        {
            get { return initialPMCGroups.Count(g => !g.HasSpawned); }
        }

        public static bool RemainingInitialPMCSpawns
        {
            get { return (initialPMCGroups.Count == 0) || initialPMCGroups.Any(g => !g.HasSpawned); }
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
            blacklistedSpawnPointIDs.Clear();

            CanSpawnPMCs = true;
        }

        public static bool IsBotFromInitialPMCSpawns(BotOwner bot)
        {
            if (bot == null)
            {
                LoggingController.LogError("Cannot check if null was part of initial PMC spawns.");
                return false;
            }

            return InitiallySpawnedPMCBots.Any(b => b.Owners.Contains(bot));
        }

        public int NumberOfBotsAllowedToSpawn()
        {
            List<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
            //LoggingController.LogInfo("Alive players: " + string.Join(", ", allPlayers.Select(p => p.Profile.Nickname + " (" + p.Id + ")")));

            BotControllerClass botControllerClass = Singleton<IBotGame>.Instance.BotsController;
            int botmax = (int)AccessTools.Field(typeof(BotControllerClass), "int_0").GetValue(botControllerClass);
            if (botmax == 0)
            {
                LoggingController.LogError("Invalid value for BotMax. Falling back to the default of 15.");
                botmax = 15;
            }

            return botmax - allPlayers.Count;
        }

        public static IEnumerable<BotOwner> RemainingAliveInitialPMCGroups()
        {
            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead)
                .Where(b => IsBotFromInitialPMCSpawns(b));
        }

        public static int RemainingAliveInitialPMCGroupCount()
        {
            return RemainingAliveInitialPMCGroups().Count();
        }

        private void Update()
        {
            if (LocationController.CurrentLocation == null)
            {
                Clear();
                return;
            }

            if (!ConfigController.Config.InitialPMCSpawns.Enabled || (LocationController.CurrentRaidSettings.BotSettings.BotAmount == EFT.Bots.EBotAmount.NoBots))
            {
                return;
            }

            if (!CanSpawnPMCs || IsSpawningPMCs || IsGeneratingPMCs || !RemainingInitialPMCSpawns)
            {
                return;
            }

            if (RemainingAliveInitialPMCGroupCount() >= ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs)
            {
                return;
            }

            float? raidET = LocationController.GetElapsedRaidTime();
            float? raidTimeRemainingFraction = LocationController.GetRaidTimeRemainingFraction();
            if (!raidET.HasValue || !raidTimeRemainingFraction.HasValue)
            {
                return;
            }

            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.InitialPMCsVsRaidET, raidTimeRemainingFraction.Value);
            if (initialPMCGroups.Count == 0)
            {
                LoggingController.LogInfo("Generating initial PMC groups...");

                int minPlayers = (int)Math.Floor(LocationController.CurrentLocation.MinPlayers * playerCountFactor);
                int maxPlayers = (int)Math.Ceiling(LocationController.CurrentLocation.MaxPlayers * playerCountFactor);

                System.Random random = new System.Random();
                maxPMCBots = random.Next(minPlayers, maxPlayers) - 1;
                LoggingController.LogInfo("Generating initial PMC groups...Generating " + maxPMCBots + " PMC's (Min: " + minPlayers + ", Max: " + maxPlayers + ")");

                // Create bot data from the server
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                generateBots(LocationController.CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty(), maxPMCBots);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                LoggingController.LogInfo("Generating initial PMC groups...done.");
                return;
            }

            if (retrySpawnTimer.ElapsedMilliseconds < ConfigController.Config.InitialPMCSpawns.SpawnRetryTime * 1000)
            {
                return;
            }

            // Ensure the raid is progressing before running anything
            if ((raidET < 1) || ((LocationController.SpawnedBotCount < LocationController.ZeroWaveTotalBotCount) && !LocationController.CurrentLocation.Name.ToLower().Contains("factory")))
            {
                return;
            }

            float minDistanceFromPlayers = playerCountFactor >= 1 ? ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersInitial : ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersDuringRaid;
            ESpawnCategoryMask allowedSpawnPointTypes = playerCountFactor >= 1 ? ESpawnCategoryMask.Player : ESpawnCategoryMask.All;
            StartCoroutine(SpawnInitialPMCs(initialPMCGroups, LocationController.CurrentLocation.SpawnPointParams, allowedSpawnPointTypes, minDistanceFromPlayers));
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
            try
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
            }
            finally
            {
                IsGeneratingPMCs = false;
            }
        }

        private IEnumerator SpawnInitialPMCs(IEnumerable<Models.BotSpawnInfo> initialPMCGroups, SpawnPointParams[] allSpawnPoints, ESpawnCategoryMask allowedSpawnPointTypes, float minDistanceFromPlayers)
        {
            try
            {
                IsSpawningPMCs = true;

                Dictionary<string, float> originalSpawnDelays = new Dictionary<string, float>();
                for (int s = 0; s < allSpawnPoints.Length; s++)
                {
                    originalSpawnDelays.Add(allSpawnPoints[s].Id, allSpawnPoints[s].DelayToCanSpawnSec);
                    allSpawnPoints[s].DelayToCanSpawnSec = 0;
                }

                int allowedSpawns = ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs - RemainingAliveInitialPMCGroupCount();
                IEnumerable<Models.BotSpawnInfo> initialPMCGroupsToSpawn = initialPMCGroups.Where(g => !g.HasSpawned).Take(allowedSpawns);

                LoggingController.LogInfo("Spawning " + initialPMCGroupsToSpawn.Count() + " initial PMC groups...");
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(initialPMCGroupsToSpawn, spawnInitialPMCsAtSpawnPoint, allowedSpawnPointTypes, minDistanceFromPlayers);

                for (int s = 0; s < allSpawnPoints.Length; s++)
                {
                    allSpawnPoints[s].DelayToCanSpawnSec = originalSpawnDelays[allSpawnPoints[s].Id];
                }

                /*yield return null;
                yield return null;

                foreach (KeyValuePair<BotZone, GClass510> keyValuePair in Singleton<IBotGame>.Instance.BotsController.Groups())
                {
                    foreach (BotGroupClass botGroupClass in keyValuePair.Value.GetGroups(true))
                    {
                        LoggingController.LogInfo("Bot Group Allies: " + string.Join(", ", botGroupClass.Allies.Select(b => b.Profile.Nickname)));
                    }
                }*/
            }
            finally
            {
                retrySpawnTimer.Restart();
                IsSpawningPMCs = false;
            }
        }

        private void spawnInitialPMCsAtSpawnPoint(Models.BotSpawnInfo initialPMCBot, ESpawnCategoryMask allowedSpawnPointTypes, float minDistanceFromPlayers)
        {
            if (initialPMCBot.HasSpawned)
            {
                return;
            }

            if (retrySpawnTimer.ElapsedMilliseconds < ConfigController.Config.InitialPMCSpawns.SpawnRetryTime * 1000)
            {
                return;
            }

            if (SpawnedInitialPMCCount > maxPMCBots)
            {
                retrySpawnTimer.Restart();
                LoggingController.LogWarning("Max PMC count of " + maxPMCBots + " already reached.");
                return;
            }

            int numberOfBotsAllowedToSpawn = NumberOfBotsAllowedToSpawn();
            if (numberOfBotsAllowedToSpawn < ConfigController.Config.InitialPMCSpawns.MinOtherBotsAllowedToSpawn)
            {
                retrySpawnTimer.Restart();
                LoggingController.LogWarning("Cannot spawn more PMC's or Scavs will not be able to spawn. Bots able to spawn: " + numberOfBotsAllowedToSpawn);
                return;
            }

            if (!initialPMCBot.SpawnPoint.HasValue && (initialPMCBot.SpawnPositions.Length == 0))
            {
                if (!initialPMCBot.TryAssignFurthestSpawnPoint(allowedSpawnPointTypes, blacklistedSpawnPointIDs.ToArray()))
                {
                    LoggingController.LogError("Could not find a valid spawn point for PMC group.");
                    return;
                }
            }

            if (initialPMCBot.SpawnPositions.Length == 0)
            {
                initialPMCBot.AssignSpawnPositionsFromSpawnPoint(initialPMCBot.Data.Count);
            }
            if (initialPMCBot.SpawnPositions.Length == 0)
            {
                if (initialPMCBot.SpawnPoint.HasValue)
                {
                    LoggingController.LogError("No valid spawn positions found for spawn point " + initialPMCBot.SpawnPoint.Value.Position.ToUnityVector3().ToString());
                    blacklistedSpawnPointIDs.Add(initialPMCBot.SpawnPoint.Value.Id);
                    initialPMCBot.SpawnPoint = null;
                }

                LoggingController.LogError("No valid spawn positions found for PMC group.");
                return;
            }

            BotControllerClass botControllerClass = Singleton<IBotGame>.Instance.BotsController;
            BotOwner closestBot = botControllerClass.ClosestBotToPoint(initialPMCBot.SpawnPositions[0]);
            if ((closestBot != null) && (Vector3.Distance(initialPMCBot.SpawnPositions[0], closestBot.Position) < minDistanceFromPlayers))
            {
                LoggingController.LogWarning("Cannot spawn PMC group at " + initialPMCBot.SpawnPositions[0].ToString() + ". Another bot is too close.");
                return;
            }

            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (Vector3.Distance(initialPMCBot.SpawnPositions[0], mainPlayer.Position) < minDistanceFromPlayers)
            {
                LoggingController.LogWarning("Cannot spawn PMC group at " + initialPMCBot.SpawnPositions[0].ToString() + ". Too close to the main player.");
                return;
            }

            Action<BotOwner> callback = new Action<BotOwner>((botOwner) =>
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spawned as an initial PMC.");
                initialPMCBot.Owners = new BotOwner[1] { botOwner };
                initialPMCBot.HasSpawned = true;
            });

            LoggingController.LogInfo("Spawning PMC group #" + (SpawnedInitialPMCCount + 1) + " at " + initialPMCBot.SpawnPositions[0] + "...");
            spawnBots(initialPMCBot.Data, initialPMCBot.SpawnPositions, callback);
        }

        private void spawnBots(GClass628 bots, Vector3[] positions, Action<BotOwner> callback)
        {
            BotSpawnerClass botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

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
