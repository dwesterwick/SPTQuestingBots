using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Patches;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PScavGenerator : BotGenerator
    {
        private BotDifficulty botDifficulty = BotDifficulty.normal;
        private Dictionary<int, float> botSpawnSchedule = new Dictionary<int, float>();

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public PScavGenerator() : base("PScav")
        {
            if (ConfigController.Config.BotSpawns.BotCapAdjustments.Enabled)
            {
                MinOtherBotsAllowedToSpawn = ConfigController.Config.BotSpawns.BotCapAdjustments.MinOtherBotsAllowedToSpawn;
            }

            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;
            RespectMaxBotCap = !ConfigController.Config.BotSpawns.AdvancedEFTBotCountManagement;

            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();
            BotDifficulty botDifficulty = locationData.CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty();

            setMaxAliveBots();
        }

        protected override void Update()
        {
            base.Update();

            if (!IsSpawningBots)
            {
                pendingSpawnPoints.Clear();
            }
        }

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

            Singleton<GameWorld>.Instance.TryGetComponent(out Components.Spawning.PMCGenerator pmcGenerator);
            if (pmcGenerator != null)
            {
                botsAllowedToSpawn -= pmcGenerator.AliveBots().Count();
                botsAllowedToSpawn -= pmcGenerator.RemainingBotsToSpawn();
            }

            return botsAllowedToSpawn;
        }

        protected override Func<Task<Models.BotSpawnInfo>> GenerateBotGroup()
        {
            return async () =>
            {
                System.Random random = new System.Random();

                // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PScavs.BotsPerGroupDistribution, random.NextDouble()));
                botsInGroup = (int)Math.Min(botsInGroup, MaxBotsToGenerate);

                ServerRequestPatch.ForcePScavCount += botsInGroup;
                Models.BotSpawnInfo group = await GenerateBotGroup(WildSpawnType.assault, botDifficulty, botsInGroup);
                group.RaidETRangeToSpawn.Min = botSpawnSchedule[GeneratedBotCount];
                
                return group;
            };
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup)
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            SpawnPointParams? spawnPoint = locationData.TryGetFurthestSpawnPointFromAllPlayers(ESpawnCategoryMask.Player, EPlayerSideMask.Savage, pendingSpawnPoints.ToArray());
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
            string locationID = locationData.CurrentLocation.Id.ToLower();
            int pScavs = (int)(locationData.CurrentLocation.MaxPlayers * ConfigController.Config.BotSpawns.PScavs.FractionOfMaxPlayers);

            if (!ConfigController.ScavRaidSettings.ContainsKey(locationID))
            {
                throw new InvalidOperationException(locationID + " not found in Scav-raid settings data from server.");
            }

            float originalEscapeTime = Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeSeconds;
            int randomAdjustment = (int)Math.Round(originalEscapeTime * ConfigController.Config.BotSpawns.PScavs.TimeRandomness / 100);

            List<float> possibleSpawnTimes = new List<float>();
            foreach (string fractionString in ConfigController.ScavRaidSettings[locationID].ReductionPercentWeights.Keys)
            {
                try
                {
                    float raidTime = float.Parse(fractionString) / 100f * originalEscapeTime;
                    int weight = ConfigController.ScavRaidSettings[locationID].ReductionPercentWeights[fractionString];

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

            System.Random random = new System.Random();
            int totalBots = 0;
            for (int wave = 0; wave < pScavs; wave++)
            {
                float selectedSpawnTime = possibleSpawnTimes[random.Next(0, possibleSpawnTimes.Count - 1)];
                float adjustedSpawnTime = Math.Min(possibleSpawnTimes.Max(), Math.Max(possibleSpawnTimes.Min(), selectedSpawnTime + random.Next(-1 * randomAdjustment, randomAdjustment)));
                
                botSpawnSchedule.Add(totalBots, adjustedSpawnTime);
                totalBots++;
            }

            IEnumerable<float> sortedSpawnTimes = botSpawnSchedule.Values.OrderBy(x => x);
            IEnumerable<string> spawnTimeTexts = sortedSpawnTimes.Select(s => TimeSpan.FromSeconds(originalEscapeTime - s).ToString("mm':'ss"));

            LoggingController.LogInfo("PScav spawn times for " + totalBots + " bots: " + string.Join(", ", spawnTimeTexts));
        }

        private void setMaxAliveBots()
        {
            string locationID = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id.ToLower();

            if (ConfigController.Config.BotSpawns.MaxAliveBots.ContainsKey(locationID))
            {
                MaxAliveBots = ConfigController.Config.BotSpawns.MaxAliveBots[locationID];
            }

            LoggingController.LogInfo("Max PScavs on the map (" + locationID + ") at the same time: " + MaxAliveBots);
        }

        private float getMinDistanceFromOtherPlayers()
        {
            if (getRaidTimeRemainingFraction() > 0.98)
            {
                return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersInitial;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersDuringRaidFactory;
            }

            return ConfigController.Config.BotSpawns.PScavs.MinDistanceFromPlayersDuringRaid;
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
