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
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Controllers.Bots
{
    public class BotGenerator : MonoBehaviour
    {
        public static bool IsClearing { get; private set; } = false;
        public static bool CanSpawnPMCs { get; private set; } = true;
        public static bool IsSpawningPMCs { get; private set; } = false;
        public static bool IsGeneratingPMCs { get; private set; } = false;
        
        private static CoroutineExtensions.EnumeratorWithTimeLimit enumeratorWithTimeLimit = new CoroutineExtensions.EnumeratorWithTimeLimit(ConfigController.Config.MaxCalcTimePerFrame);
        private static int maxAlivePMCs = 6;
        private static int maxTotalBots = 15;
        private static Stopwatch retrySpawnTimer = Stopwatch.StartNew();
        private static List<Models.BotSpawnInfo> initialPMCGroups = new List<Models.BotSpawnInfo>();

        // Stores spawn points that don't have valid NavMesh positions near them
        private static List<string> blacklistedSpawnPointIDs = new List<string>();

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private static List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public static ReadOnlyCollection<Models.BotSpawnInfo> InitialPMCBotGroups
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

        public static bool HasRemainingInitialPMCSpawns
        {
            get { return (CanSpawnPMCs && (initialPMCGroups.Count == 0)) || initialPMCGroups.Any(g => !g.HasSpawned); }
        }

        public static IEnumerator Clear()
        {
            IsClearing = true;

            if (IsSpawningPMCs)
            {
                enumeratorWithTimeLimit.Abort();

                CoroutineExtensions.EnumeratorWithTimeLimit conditionWaiter = new CoroutineExtensions.EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsSpawningPMCs, nameof(IsSpawningPMCs), 3000);

                IsSpawningPMCs = false;
            }

            if (IsGeneratingPMCs)
            {
                enumeratorWithTimeLimit.Abort();

                CoroutineExtensions.EnumeratorWithTimeLimit conditionWaiter = new CoroutineExtensions.EnumeratorWithTimeLimit(1);
                yield return conditionWaiter.WaitForCondition(() => !IsGeneratingPMCs, nameof(IsGeneratingPMCs), 3000);

                IsGeneratingPMCs = false;
            }

            maxAlivePMCs = 6;
            if (ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs.ContainsKey("default"))
            {
                maxAlivePMCs = ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs["default"];
            }

            maxTotalBots = 15;

            initialPMCGroups.Clear();
            blacklistedSpawnPointIDs.Clear();

            CanSpawnPMCs = true;

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
                StartCoroutine(Clear());
                return;
            }

            if (!ConfigController.Config.InitialPMCSpawns.Enabled || (LocationController.CurrentRaidSettings.BotSettings.BotAmount == EFT.Bots.EBotAmount.NoBots))
            {
                return;
            }

            if (!CanSpawnPMCs || IsSpawningPMCs || IsGeneratingPMCs || !HasRemainingInitialPMCSpawns)
            {
                return;
            }

            // At this point, bots are not being spawned anymore, so this data is no longer needed
            pendingSpawnPoints.Clear();

            // Don't allow too many alive PMC's to be on the map for performance and difficulty reasons
            if (RemainingAliveInitialPMCs().Count() >= maxAlivePMCs)
            {
                return;
            }

            // Ensure the total number of bots isn't too close to the bot cap for the map
            if (NumberOfBotsAllowedToSpawn() < ConfigController.Config.InitialPMCSpawns.MinOtherBotsAllowedToSpawn)
            {
                return;
            }

            float? raidET = LocationController.GetElapsedRaidTime();
            float? raidTimeRemainingFraction = LocationController.GetRaidTimeRemainingFraction();
            if (!raidET.HasValue || !raidTimeRemainingFraction.HasValue)
            {
                return;
            }

            // Determine how much to reduce the initial PMC's based on raid ET (used for Scav runs in Late to the Party)
            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.InitialPMCsVsRaidET, raidTimeRemainingFraction.Value);
            
            // Check if initial PMC groups can be spawned and if they have been generated yet
            if (CanSpawnPMCs && (initialPMCGroups.Count == 0))
            {
                LoggingController.LogInfo("Generating initial PMC groups (Raid time remaining factor: " + Math.Round(raidTimeRemainingFraction.Value, 3) + ")...");

                // Get the number of alive PMC's allowed on the map
                if (ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs.ContainsKey(LocationController.CurrentLocation.Id.ToLower()))
                {
                    maxAlivePMCs = ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs[LocationController.CurrentLocation.Id.ToLower()];
                }
                LoggingController.LogInfo("Max PMC's on the map (" + LocationController.CurrentLocation.Id + ") at the same time: " + maxAlivePMCs);

                // Get the total number of alive bots allowed on the map
                BotsController botControllerClass = Singleton<IBotGame>.Instance.BotsController;
                int botmax = (int)AccessTools.Field(typeof(BotsController), "_maxCount").GetValue(botControllerClass);
                if (botmax > 0)
                {
                    maxTotalBots = botmax;
                }
                LoggingController.LogInfo("Max total bots on the map (" + LocationController.CurrentLocation.Id + ") at the same time: " + maxTotalBots);

                // Choose the number of initial PMC's to spawn
                System.Random random = new System.Random();
                int minPlayers = (int)Math.Floor((LocationController.CurrentLocation.MinPlayers * playerCountFactor) - 1);
                int maxPlayers = (int)Math.Ceiling((LocationController.CurrentLocation.MaxPlayers * playerCountFactor) - 1);
                int maxPMCBots = random.Next(minPlayers, maxPlayers);
                LoggingController.LogInfo("Generating initial PMC groups...Generating " + maxPMCBots + " PMC's (Min: " + minPlayers + ", Max: " + maxPlayers + ")");

                if (maxPMCBots > 0)
                {
                    // If this instance is retrieved in a Task (which runs asynchronously), it can sometimes be null
                    BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

                    // Create bot data from the server
                    #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    generateBots(botSpawnerClass, LocationController.CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty(), maxPMCBots);
                    #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
                else
                {
                    CanSpawnPMCs = false;
                }

                LoggingController.LogInfo("Generating initial PMC groups...done.");
                return;
            }

            // If the previous attempt to spawn a bot failed, wait a minimum amount of time before trying again
            if (retrySpawnTimer.ElapsedMilliseconds < ConfigController.Config.InitialPMCSpawns.SpawnRetryTime * 1000)
            {
                return;
            }

            // Ensure the raid is progressing before running anything
            float? timeSinceSpawning = LocationController.GetTimeSinceSpawning();
            if ((!timeSinceSpawning.HasValue) || (timeSinceSpawning < 1))
            {
                return;
            }

            // Wait until all initial bosses have spawned before spawning inital PMC's (except for Factory)
            if ((BotRegistrationManager.SpawnedBotCount < BotRegistrationManager.ZeroWaveTotalBotCount) && !LocationController.CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return;
            }

            // Determine how far newly spawned bots need to be from other bots and where they're allowed to spawn
            float minDistanceDuringRaid = LocationController.CurrentLocation.Name.ToLower().Contains("factory") ? ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersDuringRaidFactory : ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersDuringRaid;
            float minDistanceFromPlayers = playerCountFactor >= 0.98 ? ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersInitial : minDistanceDuringRaid;
            ESpawnCategoryMask allowedSpawnPointTypes = playerCountFactor >= 0.98 ? ESpawnCategoryMask.Player : ESpawnCategoryMask.All;
            
            // Spawn PMC's
            StartCoroutine(SpawnInitialPMCs(initialPMCGroups.ToArray(), LocationController.CurrentLocation.SpawnPointParams, allowedSpawnPointTypes, minDistanceFromPlayers));
        }

        public static bool IsBotFromInitialPMCSpawns(BotOwner bot)
        {
            if (bot == null)
            {
                LoggingController.LogError("Cannot check if null was part of initial PMC spawns.");
                return false;
            }

            //LoggingController.LogInfo("Initial PMC's: " + string.Join(", ", initialPMCGroups.SelectMany(g => g.Owners.Select(o => o.Profile.Nickname))));

            return InitialPMCBotGroups.Any(b => b.Owners.Contains(bot));
        }

        public static bool TryGetInitialPMCGroup(BotOwner bot, out BotSpawnInfo matchingGroupData)
        {
            matchingGroupData = null;

            foreach (BotSpawnInfo info in initialPMCGroups)
            {
                foreach (Profile profile in info.Data.Profiles)
                {
                    if (profile.Id != bot.Profile.Id)
                    {
                        continue;
                    }

                    matchingGroupData = info;
                    return true;
                }
            }

            return false;
        }

        public static IReadOnlyCollection<BotOwner> GetSpawnGroupMembers(BotOwner bot)
        {
            IEnumerable<BotSpawnInfo> matchingSpawnGroups = InitialPMCBotGroups.Where(g => g.Owners.Contains(bot));
            if (matchingSpawnGroups.Count() == 0)
            {
                return new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
            }
            if (matchingSpawnGroups.Count() > 1)
            {
                throw new InvalidOperationException("There is more than one initial PMC spawn group with bot " + bot.Profile.Nickname);
            }

            IEnumerable<BotOwner> botFriends = matchingSpawnGroups.First().Owners.Where(i => i.Profile.Id != bot.Profile.Id);
            return new ReadOnlyCollection<BotOwner>(botFriends.ToArray());
        }

        public int NumberOfBotsAllowedToSpawn()
        {
            List<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList;
            //LoggingController.LogInfo("Alive players: " + string.Join(", ", allPlayers.Select(p => p.Profile.Nickname + " (" + p.Id + ")")));

            return maxTotalBots - allPlayers.Count;
        }

        public static IEnumerable<BotOwner> RemainingAliveInitialPMCs()
        {
            return Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead)
                .Where(b => IsBotFromInitialPMCSpawns(b));
        }

        private async Task generateBots(BotSpawner botSpawnerClass, BotDifficulty botdifficulty, int totalCount)
        {
            if (botSpawnerClass == null)
            {
                throw new NullReferenceException("Singleton<IBotGame>.Instance.BotsController.BotSpawner is null");
            }

            int botsGenerated = 0;

            try
            {
                IsGeneratingPMCs = true;

                LoggingController.LogInfo("Generating PMC bots...");

                IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

                System.Random random = new System.Random();
                int botGroup = 1;
                while (botsGenerated < totalCount)
                {
                    // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                    //int botsInGroup = random.Next(1,1);
                    int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.BotsPerGroupDistribution, random.NextDouble()));
                    botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                    // Randomly select the PMC faction (BEAR or USEC) for all of the bots in the group
                    WildSpawnType spawnType = BotBrainHelpers.pmcSpawnTypes.Random();
                    EPlayerSide spawnSide = BotBrainHelpers.GetSideForWildSpawnType(spawnType);

                    // TO DO: TriggerType and Id_spawn might not be required
                    BotSpawnParams spawnParams = new BotSpawnParams();
                    spawnParams.TriggerType = SpawnTriggerType.none;
                    spawnParams.Id_spawn = "InitialPMCGroup_" + botGroup;
                    if (botsInGroup > 1)
                    {
                        spawnParams.ShallBeGroup = new ShallBeGroupParams(true, true, botsInGroup);
                    }

                    // This causes a deadlock for some reason
                    /*if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        break;
                    }*/

                    LoggingController.LogInfo("Generating PMC group spawn #" + botGroup + " (Number of bots: " + botsInGroup + ")...");
                    try
                    {
                        GClass514 botProfileData = new GClass514(spawnSide, spawnType, botdifficulty, 0f, spawnParams);
                        GClass513 botSpawnData = await GClass513.Create(botProfileData, ibotCreator, botsInGroup, botSpawnerClass);

                        Models.BotSpawnInfo botSpawnInfo = new Models.BotSpawnInfo(botGroup, botSpawnData);
                        botSpawnInfo.UpdateOriginalSpawnTypes();

                        initialPMCGroups.Add(botSpawnInfo);
                    }
                    catch (NullReferenceException nre)
                    {
                        LoggingController.LogWarning("Generating PMC group spawn #" + botGroup + " (Number of bots: " + botsInGroup + ")...failed. Trying again...");

                        LoggingController.LogError(nre.Message);
                        LoggingController.LogError(nre.StackTrace);

                        continue;
                    }
                    catch (Exception)
                    {
                        throw;
                    }

                    botsGenerated += botsInGroup;
                    botGroup++;
                }

                LoggingController.LogInfo("Generating PMC bots...done.");
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);
            }
            finally
            {
                if (botsGenerated < totalCount)
                {
                    LoggingController.LogErrorToServerConsole("Only " + botsGenerated + " of " + totalCount + " initial PMC's were generated due to an error.");
                }

                IsGeneratingPMCs = false;
            }
        }

        private IEnumerator SpawnInitialPMCs(Models.BotSpawnInfo[] initialPMCGroups, SpawnPointParams[] allSpawnPoints, ESpawnCategoryMask allowedSpawnPointTypes, float minDistanceFromPlayers)
        {
            try
            {
                IsSpawningPMCs = true;

                // Determine how many PMC's are allowed to spawn
                int allowedSpawns = maxAlivePMCs - RemainingAliveInitialPMCs().Count();
                List<Models.BotSpawnInfo> initialPMCGroupsToSpawn = new List<BotSpawnInfo>();
                for (int i = 0; i < initialPMCGroups.Length;  i++)
                {
                    if (initialPMCGroups[i].HasSpawned)
                    {
                        continue;
                    }

                    if (initialPMCGroupsToSpawn.Sum(g => g.Count) + initialPMCGroups[i].Count > allowedSpawns)
                    {
                        break;
                    }

                    initialPMCGroupsToSpawn.Add(initialPMCGroups[i]);
                }

                if (initialPMCGroupsToSpawn.Count == 0)
                {
                    yield break;
                }

                // For some reason, we need to set the "DelayToCanSpawnSec" for at least one of the spawn points to 0, or PMC's won't spawn immediately.
                // I do this for all spawn points just to be safe. I don't understand why this is necessary. 
                Dictionary<string, float> originalSpawnDelays = new Dictionary<string, float>();
                for (int s = 0; s < allSpawnPoints.Length; s++)
                {
                    originalSpawnDelays.Add(allSpawnPoints[s].Id, allSpawnPoints[s].DelayToCanSpawnSec);
                    allSpawnPoints[s].DelayToCanSpawnSec = 0;
                }

                LoggingController.LogInfo("Trying to spawn " + initialPMCGroupsToSpawn.Count + " initial PMC group(s)...");
                enumeratorWithTimeLimit.Reset();
                yield return enumeratorWithTimeLimit.Run(initialPMCGroupsToSpawn, spawnInitialPMCsAtSpawnPoint, allowedSpawnPointTypes, minDistanceFromPlayers);
                //LoggingController.LogInfo("Trying to spawn " + initialPMCGroupsToSpawn.Count + " initial PMC groups...done.");

                // Restore the original "DelayToCanSpawnSec" values for all spawn points
                for (int s = 0; s < allSpawnPoints.Length; s++)
                {
                    allSpawnPoints[s].DelayToCanSpawnSec = originalSpawnDelays[allSpawnPoints[s].Id];
                }
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
                //LoggingController.LogError("PMC group has already spawned.");
                return;
            }

            // If the previous attempt to spawn a bot failed, wait a minimum amount of time before trying again
            if (retrySpawnTimer.ElapsedMilliseconds < ConfigController.Config.InitialPMCSpawns.SpawnRetryTime * 1000)
            {
                //LoggingController.LogWarning("Cannot spawn more PMC's right now. Time since retry-timer reset: " + retrySpawnTimer.ElapsedMilliseconds + " ms.");
                return;
            }

            // Do not allow too many bots to spawn into the map
            int numberOfBotsAllowedToSpawn = NumberOfBotsAllowedToSpawn();
            if (numberOfBotsAllowedToSpawn < ConfigController.Config.InitialPMCSpawns.MinOtherBotsAllowedToSpawn)
            {
                retrySpawnTimer.Restart();
                LoggingController.LogWarning("Cannot spawn more PMC's or Scavs will not be able to spawn. Bots able to spawn: " + numberOfBotsAllowedToSpawn);
                return;
            }

            // Try to select a valid spawn point for the first bot in the group. If one hasn't already been assigned, choose the furthest spawn point
            // from all other bots in the map. 
            if (!initialPMCBot.SpawnPoint.HasValue && (initialPMCBot.SpawnPositions.Length == 0))
            {
                if (!initialPMCBot.TryAssignFurthestSpawnPoint(allowedSpawnPointTypes, blacklistedSpawnPointIDs.ToArray()))
                {
                    LoggingController.LogError("Could not find a valid spawn point for PMC group #" + initialPMCBot.GroupNumber);
                    return;
                }
            }

            // If spawn positions for each bot in the group haven't already been selected, choose them using the assigned spawn point
            if (initialPMCBot.SpawnPositions.Length == 0)
            {
                initialPMCBot.AssignSpawnPositionsFromSpawnPoint(initialPMCBot.Data.Count, pendingSpawnPoints.ToArray());
            }
            if (initialPMCBot.SpawnPositions.Length == 0)
            {
                if (initialPMCBot.SpawnPoint.HasValue)
                {
                    LoggingController.LogError("No valid spawn positions found for spawn point " + initialPMCBot.SpawnPoint.Value.Position.ToUnityVector3().ToString());
                    blacklistedSpawnPointIDs.Add(initialPMCBot.SpawnPoint.Value.Id);
                    initialPMCBot.SpawnPoint = null;
                }

                LoggingController.LogError("No valid spawn positions found for PMC group #" + initialPMCBot.GroupNumber);
                return;
            }

            // Ensure the selected spawn position for the first bot in the group is not too close to another bot
            BotsController botControllerClass = Singleton<IBotGame>.Instance.BotsController;
            BotOwner closestBot = botControllerClass.ClosestBotToPoint(initialPMCBot.SpawnPositions[0]);
            if ((closestBot != null) && (Vector3.Distance(initialPMCBot.SpawnPositions[0], closestBot.Position) < minDistanceFromPlayers))
            {
                LoggingController.LogWarning("Cannot spawn PMC group #" + initialPMCBot.GroupNumber + " at " + initialPMCBot.SpawnPositions[0].ToString() + ". Another bot is too close.");
                initialPMCBot.SpawnPoint = null;
                initialPMCBot.SpawnPositions = new Vector3[0];
                return;
            }

            // Ensure the selected spawn position for the first bot in the group is not too close to you
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (Vector3.Distance(initialPMCBot.SpawnPositions[0], mainPlayer.Position) < minDistanceFromPlayers)
            {
                LoggingController.LogWarning("Cannot spawn PMC group #" + initialPMCBot.GroupNumber + " at " + initialPMCBot.SpawnPositions[0].ToString() + ". Too close to the main player.");
                initialPMCBot.SpawnPoint = null;
                initialPMCBot.SpawnPositions = new Vector3[0];
                return;
            }

            // Definte the callback action that runs after the bot spawns. This is needed to get the BotOwner object for the bot. 
            Action<BotOwner> callback = new Action<BotOwner>((botOwner) =>
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spawned in initial PMC group #" + initialPMCBot.GroupNumber);
                initialPMCBot.Owners.Add(botOwner);
                initialPMCBot.HasSpawned = true;
            });

            string spawnPositionText = string.Join(", ", initialPMCBot.SpawnPositions.Select(s => s.ToString()));
            LoggingController.LogInfo("Spawning PMC group #" + initialPMCBot.GroupNumber + " at " + spawnPositionText + "...");
            spawnBots(initialPMCBot.Data, initialPMCBot.SpawnPositions, callback);

            // Add the bot's spawn point to the list of other spawn points that are currently being used. That way, multiple bots won't spawn close to each
            // other when multiple initial PMC groups are spawned at the same time. 
            if (initialPMCBot.SpawnPoint.HasValue)
            {
                pendingSpawnPoints.Add(initialPMCBot.SpawnPoint.Value);
            }
        }

        private void spawnBots(GClass513 bots, Vector3[] positions, Action<BotOwner> callback)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            BotZone closestBotZone = botSpawnerClass.GetClosestZone(positions[0], out float dist);
            foreach (Vector3 position in positions)
            {
                //LoggingController.LogInfo("Adding spawn position " + position.ToString() + " for PMC group...");
                bots.AddPosition(position);
            }

            MethodInfo botSpawnMethod = typeof(BotSpawner).GetMethod("method_9", BindingFlags.Instance | BindingFlags.NonPublic);
            botSpawnMethod.Invoke(botSpawnerClass, new object[] { closestBotZone, bots, callback, botSpawnerClass.GetCancelToken() });
        }
    }
}
