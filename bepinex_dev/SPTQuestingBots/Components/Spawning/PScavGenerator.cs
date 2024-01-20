using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PScavGenerator : BotGenerator
    {
        private bool hasGeneratedBotGroups = false;
        private bool isGeneratingBotGroups = false;

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public PScavGenerator() : base("PScav")
        {
            MinOtherBotsAllowedToSpawn = ConfigController.Config.BotSpawns.MinOtherBotsAllowedToSpawn;
            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;

            setMaxAliveBots();
            ConfigController.GetScavRaidSettings();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsSpawningBots)
            {
                pendingSpawnPoints.Clear();
            }

            if (isGeneratingBotGroups || HasGeneratedBotGroups())
            {
                return;
            }

            // Check if PScavs are allowed to spawn in the raid
            if (!PlayerWantsBotsInRaid() && !ConfigController.Config.Debug.AlwaysSpawnPScavs)
            {
                return;
            }

            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if ((pmcGenerator != null) && !pmcGenerator.HasGeneratedBotGroups())
            {
                return;
            }

            generateBotGroups();
        }

        public override bool HasGeneratedBotGroups() => hasGeneratedBotGroups;
        protected override void GenerateInitialBotGroups() { }

        protected override bool CanSpawnBots()
        {
            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if (pmcGenerator == null)
            {
                return true;
            }

            if (pmcGenerator.HasRemainingSpawns)
            {
                return false;
            }

            if (!pmcGenerator.CanSpawnAdditionalBots())
            {
                return false;
            }

            return true;
        }

        private void generateBotGroups()
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            BotDifficulty botDifficulty = locationData.CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty();
            int pScavs = locationData.CurrentLocation.MaxPlayers;

            LoggingController.LogInfo("Generating " + pScavs + " PScavs...");

            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            generateBotGroupsTask(botDifficulty, pScavs);
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task generateBotGroupsTask(BotDifficulty botdifficulty, int totalCount)
        {
            int botsGenerated = 0;

            try
            {
                LoggingController.LogInfo("Generating PScavs...");
                isGeneratingBotGroups = true;

                // Ensure the PMC-conversion chances have remained at 0%
                ConfigController.AdjustPMCConversionChances(0, true);

                System.Random random = new System.Random();
                int botGroup = 1;
                //while (botsGenerated < 1)
                while (botsGenerated < totalCount)
                {
                    // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                    int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PScavs.BotsPerGroupDistribution, random.NextDouble()));
                    botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                    ServerRequestPatch.ForcePScavCount++;
                    Models.BotSpawnInfo group = await GenerateBotGroup(WildSpawnType.assault, botdifficulty, botsInGroup);
                    BotGroups.Add(group);

                    botsGenerated += botsInGroup;
                    botGroup++;
                }

                LoggingController.LogInfo("Generating PScavs...done.");
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
                    LoggingController.LogErrorToServerConsole("Only " + botsGenerated + " of " + totalCount + " PScavs were generated due to an error.");
                }

                hasGeneratedBotGroups = true;
                isGeneratingBotGroups = false;
            }
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup)
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            SpawnPointParams? spawnPoint = locationData.TryGetFurthestSpawnPointFromAllPlayers(ESpawnCategoryMask.All, pendingSpawnPoints.ToArray());
            if (!spawnPoint.HasValue)
            {
                LoggingController.LogError("Could not find a valid spawn point for PScav group");
                return Enumerable.Empty<Vector3>();
            }

            IEnumerable<Vector3> spawnPositions = locationData.GetNearestSpawnPoints(spawnPoint.Value.Position.ToUnityVector3(), botGroup.Data.Count, pendingSpawnPoints.ToArray())
                .Select(p => p.Position.ToUnityVector3());

            if (!spawnPositions.Any())
            {
                LoggingController.LogError("No valid spawn positions found for spawn point " + spawnPoint.Value.Position.ToUnityVector3().ToString());
                return Enumerable.Empty<Vector3>();
            }

            if (locationData.AreAnyPositionsCloseToOtherPlayers(spawnPositions, getMinDistanceFromOtherPlayers()))
            {
                LoggingController.LogWarning("Cannot spawn " + BotTypeName + " group at " + spawnPoint.Value.Position.ToUnityVector3().ToString() + ". Other players are too close.");
                return Enumerable.Empty<Vector3>();
            }

            // Add the bot's spawn point to the list of other spawn points that are currently being used. That way, multiple bots won't spawn close to each
            // other when multiple initial PMC groups are spawned at the same time. 
            if (spawnPoint.HasValue)
            {
                pendingSpawnPoints.Add(spawnPoint.Value);
            }

            return spawnPositions;
        }

        private void setMaxAliveBots()
        {
            string locationID = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id.ToLower();

            if (ConfigController.Config.BotSpawns.MaxAliveInitialPMCs.ContainsKey(locationID))
            {
                MaxAliveBots = ConfigController.Config.BotSpawns.MaxAliveInitialPMCs[locationID];
            }
            LoggingController.LogInfo("Max PScavs on the map (" + locationID + ") at the same time: " + MaxAliveBots);
        }

        private float getMinDistanceFromOtherPlayers()
        {
            if (getRaidTimeRemainingFraction() > 0.98)
            {
                return ConfigController.Config.BotSpawns.MinDistanceFromPlayersInitial;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return ConfigController.Config.BotSpawns.MinDistanceFromPlayersDuringRaidFactory;
            }

            return ConfigController.Config.BotSpawns.MinDistanceFromPlayersDuringRaid;
        }

        private float getRaidTimeRemainingFraction()
        {
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }

            return (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewEscapeTimeMinutes / Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeMinutes;
        }
    }
}
