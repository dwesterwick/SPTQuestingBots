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
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PMCGenerator : BotGenerator
    {
        private static int maxAlivePMCs = 6;
        private static Stopwatch retrySpawnTimer = Stopwatch.StartNew();
        
        // Stores spawn points that don't have valid NavMesh positions near them
        private static List<string> blacklistedSpawnPointIDs = new List<string>();

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private static List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public PMCGenerator() : base("PMC")
        {

        }

        private void Awake()
        {
            // Check if PMC's are allowed to spawn in the raid
            if (!PlayerWantsBotsInRaid() && !ConfigController.Config.Debug.AlwaysSpawnPMCs)
            {
                return;
            }

            generateBots();
        }

        private void Update()
        {
            if (!HasGeneratedBots || IsSpawningBots || !HasRemainingSpawns)
            {
                return;
            }

            // At this point, bots are not being spawned anymore, so this data is no longer needed
            pendingSpawnPoints.Clear();

            // Don't allow too many alive PMC's to be on the map for performance and difficulty reasons
            if (AliveBots().Count() >= maxAlivePMCs)
            {
                return;
            }

            // Ensure the total number of bots isn't too close to the bot cap for the map
            if (NumberOfBotsAllowedToSpawn() < ConfigController.Config.InitialPMCSpawns.MinOtherBotsAllowedToSpawn)
            {
                return;
            }

            // If the previous attempt to spawn a bot failed, wait a minimum amount of time before trying again
            if (retrySpawnTimer.ElapsedMilliseconds < ConfigController.Config.InitialPMCSpawns.SpawnRetryTime * 1000)
            {
                return;
            }

            // Ensure the raid is progressing before running anything
            float timeSinceSpawning = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetSecondsSinceSpawning();
            if (timeSinceSpawning < 0.01)
            {
                return;
            }

            // Wait until all initial bosses have spawned before spawning inital PMC's (except for Factory)
            if 
            (
                PlayerWantsBotsInRaid()
                && (Controllers.BotRegistrationManager.SpawnedBotCount < BotRegistrationManager.ZeroWaveTotalBotCount)
                && !Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory")
            )
            {
                return;
            }

            float raidTimeRemainingFraction;
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                raidTimeRemainingFraction = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }
            else
            {
                raidTimeRemainingFraction = (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewEscapeTimeMinutes / Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeMinutes;
            }

            // Determine how much to reduce the initial PMC's based on raid ET (used for Scav runs in Late to the Party)
            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.InitialPMCsVsRaidET, raidTimeRemainingFraction);

            // Determine how far newly spawned bots need to be from other bots and where they're allowed to spawn
            float minDistanceDuringRaid = Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory") ? ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersDuringRaidFactory : ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersDuringRaid;
            float minDistanceFromPlayers = playerCountFactor >= 0.98 ? ConfigController.Config.InitialPMCSpawns.MinDistanceFromPlayersInitial : minDistanceDuringRaid;
            ESpawnCategoryMask allowedSpawnPointTypes = playerCountFactor >= 0.98 ? ESpawnCategoryMask.Player : ESpawnCategoryMask.All;
            
            // Spawn PMC's
            StartCoroutine(SpawnInitialPMCs(BotGroups.ToArray(), Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.SpawnPointParams, allowedSpawnPointTypes, minDistanceFromPlayers));
        }

        private void generateBots()
        {
            float raidTimeRemainingFraction;
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                raidTimeRemainingFraction = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }
            else
            {
                raidTimeRemainingFraction = (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewEscapeTimeMinutes / Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeMinutes;
            }

            // Determine how much to reduce the initial PMC's based on raid ET (used for Scav runs in Late to the Party)
            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.InitialPMCsVsRaidET, raidTimeRemainingFraction);

            LoggingController.LogInfo("Generating initial PMC groups (Raid time remaining factor: " + Math.Round(raidTimeRemainingFraction, 3) + ")...");

            // Get the number of alive PMC's allowed on the map
            if (ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs.ContainsKey(Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Id.ToLower()))
            {
                maxAlivePMCs = ConfigController.Config.InitialPMCSpawns.MaxAliveInitialPMCs[Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Id.ToLower()];
            }
            LoggingController.LogInfo("Max PMC's on the map (" + Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Id + ") at the same time: " + maxAlivePMCs);

            // Choose the number of initial PMC's to spawn
            System.Random random = new System.Random();
            int pmcOffset = Singleton<GameWorld>.Instance.GetComponent<LocationData>().IsScavRun ? 0 : 1;
            int minPlayers = (int)Math.Floor((Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.MinPlayers * playerCountFactor) - pmcOffset);
            int maxPlayers = (int)Math.Ceiling((Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.MaxPlayers * playerCountFactor) - pmcOffset);
            int maxPMCBots = random.Next(minPlayers, maxPlayers);

            LoggingController.LogInfo("Generating initial PMC groups...Generating " + maxPMCBots + " PMC's (Min: " + minPlayers + ", Max: " + maxPlayers + ")");

            if (maxPMCBots > 0)
            {
                // Create bot data from the server
                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                generateBotGroups(Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty(), maxPMCBots);
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            //LoggingController.LogInfo("Generating initial PMC groups...done.");
            HasGeneratedBots = true;
        }

        private async Task generateBotGroups(BotDifficulty botdifficulty, int totalCount)
        {
            int botsGenerated = 0;

            try
            {
                IsGeneratingBots = true;

                LoggingController.LogInfo("Generating PMC bots...");

                // Ensure the PMC-conversion chances have remained at 0%
                ConfigController.AdjustPMCConversionChances(0, true);

                System.Random random = new System.Random();
                int botGroup = 1;
                //while (botsGenerated < 1)
                while (botsGenerated < totalCount)
                {
                    // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                    int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.InitialPMCSpawns.BotsPerGroupDistribution, random.NextDouble()));
                    botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                    // Randomly select the PMC faction (BEAR or USEC) for all of the bots in the group
                    WildSpawnType spawnType = Helpers.BotBrainHelpers.pmcSpawnTypes.Random();

                    Models.BotSpawnInfo group = await GenerateBotGroup(spawnType, botdifficulty, botsInGroup);
                    BotGroups.Add(group);

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

                IsGeneratingBots = false;
            }
        }

        private IEnumerator SpawnInitialPMCs(Models.BotSpawnInfo[] initialPMCGroups, SpawnPointParams[] allSpawnPoints, ESpawnCategoryMask allowedSpawnPointTypes, float minDistanceFromPlayers)
        {
            try
            {
                IsSpawningBots = true;

                // Determine how many PMC's are allowed to spawn
                int allowedSpawns = maxAlivePMCs - AliveBots().Count();
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
                IsSpawningBots = false;
            }
        }

        private void spawnInitialPMCsAtSpawnPoint(Models.BotSpawnInfo initialPMCGroup, ESpawnCategoryMask allowedSpawnPointTypes, float minDistanceFromPlayers)
        {
            if (initialPMCGroup.HasSpawned)
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
            if (!initialPMCGroup.SpawnPoint.HasValue && (initialPMCGroup.SpawnPositions.Length == 0))
            {
                if (!initialPMCGroup.TryAssignFurthestSpawnPoint(allowedSpawnPointTypes, blacklistedSpawnPointIDs.ToArray()))
                {
                    LoggingController.LogError("Could not find a valid spawn point for PMC group");
                    return;
                }
            }

            // If spawn positions for each bot in the group haven't already been selected, choose them using the assigned spawn point
            if (initialPMCGroup.SpawnPositions.Length == 0)
            {
                initialPMCGroup.AssignSpawnPositionsFromSpawnPoint(initialPMCGroup.Data.Count, pendingSpawnPoints.ToArray());
            }
            if (initialPMCGroup.SpawnPositions.Length == 0)
            {
                if (initialPMCGroup.SpawnPoint.HasValue)
                {
                    LoggingController.LogError("No valid spawn positions found for spawn point " + initialPMCGroup.SpawnPoint.Value.Position.ToUnityVector3().ToString());
                    blacklistedSpawnPointIDs.Add(initialPMCGroup.SpawnPoint.Value.Id);
                    initialPMCGroup.SpawnPoint = null;
                }

                LoggingController.LogError("No valid spawn positions found for PMC group");
                return;
            }

            // Ensure the selected spawn position for the first bot in the group is not too close to another bot
            BotsController botControllerClass = Singleton<IBotGame>.Instance.BotsController;
            BotOwner closestBot = botControllerClass.ClosestBotToPoint(initialPMCGroup.SpawnPositions[0]);
            if ((closestBot != null) && (Vector3.Distance(initialPMCGroup.SpawnPositions[0], closestBot.Position) < minDistanceFromPlayers))
            {
                LoggingController.LogWarning("Cannot spawn PMC group at " + initialPMCGroup.SpawnPositions[0].ToString() + ". Another bot is too close.");
                initialPMCGroup.SpawnPoint = null;
                initialPMCGroup.SpawnPositions = new Vector3[0];
                return;
            }

            // Ensure the selected spawn position for the first bot in the group is not too close to you
            Player mainPlayer = Singleton<GameWorld>.Instance.MainPlayer;
            if (Vector3.Distance(initialPMCGroup.SpawnPositions[0], mainPlayer.Position) < minDistanceFromPlayers)
            {
                LoggingController.LogWarning("Cannot spawn PMC group at " + initialPMCGroup.SpawnPositions[0].ToString() + ". Too close to the main player.");
                initialPMCGroup.SpawnPoint = null;
                initialPMCGroup.SpawnPositions = new Vector3[0];
                return;
            }

            string spawnPositionText = string.Join(", ", initialPMCGroup.SpawnPositions.Select(s => s.ToString()));
            LoggingController.LogInfo("Spawning PMC group at " + spawnPositionText + "...");

            SpawnBots(initialPMCGroup, initialPMCGroup.SpawnPositions);

            // Add the bot's spawn point to the list of other spawn points that are currently being used. That way, multiple bots won't spawn close to each
            // other when multiple initial PMC groups are spawned at the same time. 
            if (initialPMCGroup.SpawnPoint.HasValue)
            {
                pendingSpawnPoints.Add(initialPMCGroup.SpawnPoint.Value);
            }
        }
    }
}
