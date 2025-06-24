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
using SPTQuestingBots.Patches;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PScavGenerator : BotGenerator
    {
        private static System.Random random = new System.Random();

        private Dictionary<int, float> botSpawnSchedule = new Dictionary<int, float>();

        public PScavGenerator() : base("PScav") { }

        protected override void Init()
        {
            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;

            setMaxAliveBots();
        }

        protected override void Refresh() { }

        protected override int GetMaxGeneratedBots()
        {
            // Check if PMC's are allowed to spawn in the raid
            if (!PlayerWantsBotsInRaid() && !ConfigController.Config.Debug.AlwaysSpawnPScavs)
            {
                return 0;
            }

            ConfigController.GetScavRaidSettings();
            createBotSpawnSchedule();

            return botSpawnSchedule.Count;
        }

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

        protected override int GetNumberOfBotsAllowedToSpawn()
        {
            int botsAllowedToSpawn = BotsAllowedToSpawnForGeneratorType();

            // Ensure all PMC's have spawned first
            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if (pmcGenerator != null)
            {
                botsAllowedToSpawn -= pmcGenerator.AliveBots().Count();
                botsAllowedToSpawn -= pmcGenerator.RemainingBotsToSpawn();
            }

            return botsAllowedToSpawn;
        }

        protected override async Task<Models.BotSpawnInfo> GenerateBotGroupTask()
        {
            // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
            int botsInGroup = (int)Math.Round(ConfigController.GetValueFromTotalChanceFraction(ConfigController.Config.BotSpawns.PScavs.BotsPerGroupDistribution, random.NextDouble()));
            botsInGroup = (int)Math.Min(botsInGroup, MaxBotsToGenerate);
            botsInGroup = (int)Math.Max(botsInGroup, 1);

            // Determine the difficulty for the new bot group
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            BotDifficulty botDifficulty = GetBotDifficulty(locationData.CurrentRaidSettings.WavesSettings.BotDifficulty, ConfigController.Config.BotSpawns.PScavs.BotDifficultyAsOnline);

            // Force the server to generate a player Scav
            RaidHelpers.ForcePScavs = true;
            Models.BotSpawnInfo group = await GenerateBotGroup(WildSpawnType.assault, botDifficulty, botsInGroup);
            RaidHelpers.ForcePScavs = false;

            // Set the minimum and maximum spawn times for the PScav group
            float minTimeRemaining = ConfigController.Config.BotSpawns.PScavs.MinRaidTimeRemaining;
            group.RaidETRangeToSpawn.Min = botSpawnSchedule[GeneratedBotCount];
            group.RaidETRangeToSpawn.Max = RaidHelpers.OriginalEscapeTimeSeconds - minTimeRemaining;

            return group;
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup)
        {
            float minDistanceFromOtherPlayers = ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersDuringRaidFactory + 5;
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            string[] allGeneratedProfileIDs = GetAllGeneratedBotProfileIDs().ToArray();
            Vector3[] positionsToAvoid = Singleton<GameWorld>.Instance.AllAlivePlayersList
                .Where(p => !p.IsAI || allGeneratedProfileIDs.Contains(p.ProfileId))
                .Select(p => p.Position)
                .Concat(PendingSpawnPoints.Select(p => p.Position.ToUnityVector3()))
                .ToArray();

            // Find a spawn location for the bot group that is as far from other players and bots as possible
            SpawnPointParams[] excludedSpawnpoints = PendingSpawnPoints
                .SelectMany(s => locationData.GetNearbySpawnPoints(s.Position, minDistanceFromOtherPlayers)).ToArray();
            SpawnPointParams? spawnPoint = locationData.TryGetFurthestSpawnPointFromPositions(positionsToAvoid, ESpawnCategoryMask.Player, EPlayerSideMask.All, excludedSpawnpoints, minDistanceFromOtherPlayers);
            if (!spawnPoint.HasValue)
            {
                LoggingController.LogWarning("Could not find a spawn point for PScav group");
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
            if (AreAnyPositionsCloseToAnyGeneratedBots(spawnPositions, getMinDistanceFromOtherPlayers(), out float distance))
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

        private void createBotSpawnSchedule()
        {
            // Get the current location ID and ensure there are SPT Scav-raid raid-time-reduction settings for it
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            string locationID = locationData.CurrentLocation.Id.ToLower();
            if (!ConfigController.ScavRaidSettings.ContainsKey(locationID))
            {
                LoggingController.LogError("Cannot find location " + locationID + " in Scav-raid settings data from server. Falling back to default settings.");
                locationID = "default";

                if (!ConfigController.ScavRaidSettings.ContainsKey(locationID))
                {
                    throw new InvalidOperationException(locationID + " not found in Scav-raid settings data from server.");
                }
            }

            float originalEscapeTime = RaidHelpers.OriginalEscapeTimeSeconds;
            int totalPScavs = (int)(locationData.CurrentLocation.MaxPlayers * ConfigController.Config.BotSpawns.PScavs.FractionOfMaxPlayers);

            // Parse the SPT raid-time-reduction settings
            List<float> possibleSpawnTimes = new List<float>();
            foreach (string fractionString in ConfigController.ScavRaidSettings[locationID].ReductionPercentWeights.Keys)
            {
                try
                {
                    float raidTime = float.Parse(fractionString) / 100f * originalEscapeTime;
                    int weight = ConfigController.ScavRaidSettings[locationID].ReductionPercentWeights[fractionString];

                    // Add the same number of entries to the List as the weight value for the reduction percentage. This makes the
                    // reduction percentage more likely to be selected when creating the spawn schedule.
                    for (int i = 0; i < weight; i++)
                    {
                        possibleSpawnTimes.Add(raidTime);
                    }
                }
                catch (FormatException)
                {
                    LoggingController.LogError("Key \"" + fractionString + "\" could not be parsed for location " + locationID);
                }
            }

            int maxSpawnTimeAdjustment = (int)Math.Round(originalEscapeTime * ConfigController.Config.BotSpawns.PScavs.TimeRandomness / 100);

            // Create the spawn schedule
            for (int pScav = 0; pScav < totalPScavs; pScav++)
            {
                float randomSpawnTime = possibleSpawnTimes[random.Next(0, possibleSpawnTimes.Count - 1)];
                float adjustedRandomSpawnTime = randomSpawnTime + random.Next(-1 * maxSpawnTimeAdjustment, maxSpawnTimeAdjustment);

                botSpawnSchedule.Add(pScav, Math.Min(possibleSpawnTimes.Max(), Math.Max(possibleSpawnTimes.Min(), adjustedRandomSpawnTime)));
            }

            // Write the spawn schedule to the game console for debugging
            IEnumerable<float> sortedSpawnTimes = botSpawnSchedule.Values.OrderBy(x => x);
            IEnumerable<string> spawnTimeTexts = sortedSpawnTimes.Select(s => TimeSpan.FromSeconds(originalEscapeTime - s).ToString("mm':'ss"));
            LoggingController.LogInfo("Spawn times for " + totalPScavs + " PScavs: " + string.Join(", ", spawnTimeTexts));
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

            LoggingController.LogInfo("Max PScavs on the map (" + locationID + ") at the same time: " + MaxAliveBots);
        }

        private float getMinDistanceFromOtherPlayers()
        {
            if (RaidHelpers.IsBeginningOfRaid() || RaidHelpers.HumanPlayersRecentlySpawned())
            {
                return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersInitial;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersDuringRaidFactory;
            }

            return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersDuringRaid;
        }
    }
}
