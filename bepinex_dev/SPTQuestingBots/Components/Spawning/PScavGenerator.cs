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
        private Dictionary<int, float> botSpawnSchedule = new Dictionary<int, float>();

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public PScavGenerator() : base("PScav")
        {
            MinOtherBotsAllowedToSpawn = ConfigController.Config.BotSpawns.MinOtherBotsAllowedToSpawn;
            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;

            setMaxAliveBots();

            ConfigController.GetScavRaidSettings();
            createBotSpawnSchedule();
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

            LoggingController.LogInfo("Generating " + botSpawnSchedule.Count + " PScavs...");

            #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            generateBotGroupsTask(botDifficulty, botSpawnSchedule.Count);
            #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private async Task generateBotGroupsTask(BotDifficulty botdifficulty, int totalCount)
        {
            int botsGenerated = 0;

            try
            {
                isGeneratingBotGroups = true;

                // Ensure the PMC-conversion chances have remained at 0%
                ConfigController.AdjustPMCConversionChances(0, true);

                System.Random random = new System.Random();
                int botGroup = 0;
                while (botsGenerated < totalCount)
                {
                    // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                    int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PScavs.BotsPerGroupDistribution, random.NextDouble()));
                    botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                    ServerRequestPatch.ForcePScavCount++;
                    Models.BotSpawnInfo group = await GenerateBotGroup(WildSpawnType.assault, botdifficulty, botsInGroup);
                    group.RaidETRangeToSpawn.Min = botSpawnSchedule[botGroup];
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

        private void createBotSpawnSchedule()
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            int pScavs = (int)(locationData.CurrentLocation.MaxPlayers * ConfigController.Config.BotSpawns.PScavs.FractionOfMaxPlayers);

            if (!ConfigController.ScavRaidSettings.ContainsKey(locationData.CurrentLocation.Id))
            {
                throw new InvalidOperationException(locationData.CurrentLocation.Id + " not found in Scav-raid settings data from server.");
            }

            float originalEscapeTime = Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds;
            int randomAdjustment = (int)Math.Round(originalEscapeTime * 0.1);

            Dictionary<float, int> spawnWeights = new Dictionary<float, int>();
            float totalWeight = 0;
            foreach (string fractionString in ConfigController.ScavRaidSettings[locationData.CurrentLocation.Id].ReductionPercentWeights.Keys)
            {
                try
                {
                    float raidTime = float.Parse(fractionString) / 100f * originalEscapeTime;
                    int weight = ConfigController.ScavRaidSettings[locationData.CurrentLocation.Id].ReductionPercentWeights[fractionString];

                    spawnWeights.Add(raidTime, weight);

                    totalWeight += weight;
                }
                catch (FormatException)
                {
                    LoggingController.LogError("Key \"" + fractionString + "\" could not be parsed for location " + locationData.CurrentLocation.Id);
                }
            }

            System.Random random = new System.Random();
            int totalBots = 0;
            foreach (float raidET in spawnWeights.Keys)
            {
                int bots = (int)Math.Ceiling(spawnWeights[raidET] / totalWeight * pScavs);

                for (int bot = 0; bot < bots; bot++)
                {
                    float spawnTime = Math.Min(spawnWeights.Keys.Max(), Math.Max(spawnWeights.Keys.Min(), raidET + random.Next(-1 * randomAdjustment, randomAdjustment)));
                    botSpawnSchedule.Add(totalBots, spawnTime);
                    totalBots++;
                }
            }

            LoggingController.LogInfo("PScav spawn times for " + totalBots + " bots: " + string.Join(", ", botSpawnSchedule.Values));
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
