using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PMCGenerator : BotGenerator
    {
        private static System.Random random = new System.Random();

        public PMCGenerator() : base("PMC") { }

        protected override void Init()
        {
            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;

            setMaxAliveBots();
        }

        protected override void Refresh() { }

        protected override bool CanSpawnBots() => true;
        protected override int GetNumberOfBotsAllowedToSpawn() => BotsAllowedToSpawnForGeneratorType();

        protected override int GetMaxGeneratedBots()
        {
            // Check if PMC's are allowed to spawn in the raid
            if (!PlayerWantsBotsInRaid() && !ConfigController.Config.Debug.AlwaysSpawnPMCs)
            {
                return 0;
            }

            // Determine how many total PMC's to spawn (reduced for Scav raids)
            Configuration.MinMaxConfig pmcCountRange = getPMCCount();
            int pmcCount = random.Next((int)pmcCountRange.Min, (int)pmcCountRange.Max);

            // There must be at least 1 PMC still in the map or PScavs will not be allowed to join in live Tarkov
            pmcCount = Math.Max(1, pmcCount);

            LoggingController.LogInfo(pmcCount + " initial PMC groups will be generated (Min: " + pmcCountRange.Min + ", Max: " + pmcCountRange.Max + ")");

            return pmcCount;
        }

        protected override async Task<Models.BotSpawnInfo> GenerateBotGroupTask()
        {
            // Spawn smaller PMC groups later in raids
            double groupSizeFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayersVsRaidET, RaidHelpers.GetRaidTimeRemainingFraction());

            // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
            int botsInGroup = (int)Math.Round(ConfigController.GetValueFromTotalChanceFraction(ConfigController.Config.BotSpawns.PMCs.BotsPerGroupDistribution, random.NextDouble()));
            botsInGroup = (int)Math.Ceiling(botsInGroup * Math.Min(1, groupSizeFactor));
            botsInGroup = (int)Math.Min(botsInGroup, MaxBotsToGenerate);
            botsInGroup = (int)Math.Max(botsInGroup, 1);

            // Determine the difficulty for the new bot group
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            BotDifficulty botDifficulty = GetBotDifficulty(locationData.CurrentRaidSettings.WavesSettings.BotDifficulty, ConfigController.Config.BotSpawns.PMCs.BotDifficultyAsOnline);

            // Randomly select the PMC faction (BEAR or USEC) for all of the bots in the group
            WildSpawnType spawnType = WildSpawnType.pmcBEAR;
            if (random.Next(1, 100) <= ConfigController.GetUSECChance())
            {
                spawnType = WildSpawnType.pmcUSEC;
            }

            Models.BotSpawnInfo group = await GenerateBotGroup(spawnType, botDifficulty, botsInGroup);

            // Set the maximum spawn time for the PMC group
            float minTimeRemaining = ConfigController.Config.BotSpawns.PMCs.MinRaidTimeRemaining;
            group.RaidETRangeToSpawn.Max = RaidHelpers.OriginalEscapeTimeSeconds - minTimeRemaining;

            return group;
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup)
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            EPlayerSideMask playerMask = RaidHelpers.IsBeginningOfRaid() ? EPlayerSideMask.Pmc : EPlayerSideMask.All;
            float minDistanceFromOtherPlayers = getMinDistanceFromOtherPlayers() + 5;

            string[] allGeneratedProfileIDs = GetAllGeneratedBotProfileIDs().ToArray();
            Vector3[] positionsToAvoid = Singleton<GameWorld>.Instance.AllAlivePlayersList
                .Where(p => !p.IsAI || allGeneratedProfileIDs.Contains(p.ProfileId))
                .Select(p => p.Position)
                .Concat(PendingSpawnPoints.Select(p => p.Position.ToUnityVector3()))
                .ToArray();

            // Find a spawn location for the bot group that is as far from other players and bots as possible
            SpawnPointParams[] excludedSpawnpoints = PendingSpawnPoints
                .SelectMany(s => locationData.GetNearbySpawnPoints(s.Position, minDistanceFromOtherPlayers)).ToArray();
            SpawnPointParams? spawnPoint = locationData.TryGetFurthestSpawnPointFromPositions(positionsToAvoid, ESpawnCategoryMask.Player, playerMask, excludedSpawnpoints, minDistanceFromOtherPlayers);
            if (!spawnPoint.HasValue)
            {
                LoggingController.LogWarning("Could not find a spawn point for PMC group");
                return Enumerable.Empty<Vector3>();
            }

            // Create a list of spawn points at the selected location
            IEnumerable<Vector3> spawnPositions = locationData.GetNearestSpawnPoints(spawnPoint.Value.Position.ToUnityVector3(), botGroup.Data.Count, excludedSpawnpoints)
                .Select(p => p.Position.ToUnityVector3());

            if (!spawnPositions.Any())
            {
                LoggingController.LogError("No valid spawn positions found for spawn point " + spawnPoint.Value.Position.ToUnityVector3().ToString());
                return Enumerable.Empty<Vector3>();
            }

            // Ensure none of the spawn points are too close to other players or bots
            if (locationData.AreAnyPositionsCloseToOtherPlayers(spawnPositions, getMinDistanceFromOtherPlayers(), out float distance))
            {
                LoggingController.LogWarning("Cannot spawn " + BotTypeName + " group at " + spawnPoint.Value.Position.ToUnityVector3().ToString() + ". Another player is " + distance + "m away.");
                return Enumerable.Empty<Vector3>();
            }

            // Add the bot's spawn point to the list of other spawn points that are currently being used. That way, multiple bots won't spawn close to each
            // other when multiple initial PMC groups are spawned at the same time. 
            if (spawnPoint.HasValue)
            {
                PendingSpawnPoints.Add(spawnPoint.Value);
            }

            return spawnPositions;
        }

        private void setMaxAliveBots()
        {
            string locationID = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id.ToLower();

            if (ConfigController.Config.BotSpawns.MaxAliveBots.ContainsKey(locationID))
            {
                MaxAliveBots = ConfigController.Config.BotSpawns.MaxAliveBots[locationID];
            }
            else if (ConfigController.Config.BotSpawns.MaxAliveBots.ContainsKey("default"))
            {
                MaxAliveBots = ConfigController.Config.BotSpawns.MaxAliveBots["default"];
            }

            LoggingController.LogInfo("Max PMC's on the map (" + locationID + ") at the same time: " + MaxAliveBots);
        }

        private Configuration.MinMaxConfig getPMCCount()
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            // Determine how much to reduce the initial PMC's based on raid ET (used for Scav runs)
            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayersVsRaidET, RaidHelpers.GetRaidTimeRemainingFraction());
            playerCountFactor *= ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayers;

            // Choose the number of initial PMC's to spawn
            int pmcOffset = RaidHelpers.IsScavRun ? 0 : 1;
            int minPlayers = (int)Math.Floor((locationData.CurrentLocation.MinPlayers * playerCountFactor) - pmcOffset);
            int maxPlayers = (int)Math.Ceiling((locationData.CurrentLocation.MaxPlayers * playerCountFactor) - pmcOffset);

            return new Configuration.MinMaxConfig(minPlayers, maxPlayers);
        }

        private float getMinDistanceFromOtherPlayers()
        {
            if (RaidHelpers.IsBeginningOfRaid() || RaidHelpers.HumanPlayersRecentlySpawned())
            {
                return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersInitial;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersDuringRaidFactory;
            }

            return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersDuringRaid;
        }
    }
}
