using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using EFT.UI;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Components.Spawning
{
    public class PMCGenerator : BotGenerator
    {
        private bool hasGeneratedBotGroups = false;

        // Temporarily stores spawn points for bots while trying to spawn several of them
        private List<SpawnPointParams> pendingSpawnPoints = new List<SpawnPointParams>();

        public PMCGenerator() : base("PMC")
        {
            if (ConfigController.Config.BotSpawns.BotCapAdjustments.Enabled)
            {
                MinOtherBotsAllowedToSpawn = ConfigController.Config.BotSpawns.BotCapAdjustments.MinOtherBotsAllowedToSpawn;
            }

            RetryTimeSeconds = ConfigController.Config.BotSpawns.SpawnRetryTime;

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

        public override bool HasGeneratedBotGroups() => hasGeneratedBotGroups;
        protected override bool CanSpawnBots() => true;
        protected override int NumberOfBotsAllowedToSpawn() => BotsAllowedToSpawnForGeneratorType();

        protected override void GenerateInitialBotGroups()
        {
            // Check if PMC's are allowed to spawn in the raid
            if (!PlayerWantsBotsInRaid() && !ConfigController.Config.Debug.AlwaysSpawnPMCs)
            {
                return;
            }

            System.Random random = new System.Random();
            Configuration.MinMaxConfig pmcCountRange = getPMCCount();
            int pmcCount = random.Next((int)pmcCountRange.Min, (int)pmcCountRange.Max);

            if (pmcCount > 0)
            {
                LoggingController.LogInfo(pmcCount + " initial PMC groups will be generated (Min: " + pmcCountRange.Min + ", Max: " + pmcCountRange.Max + ")");

                BotDifficulty botDifficulty = Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentRaidSettings.WavesSettings.BotDifficulty.ToBotDifficulty();

                #pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                //generateBotGroupsTask(botDifficulty, pmcCount);
                AddBotGenerationTask(generateBotGroupsTask(botDifficulty, pmcCount));
                #pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                LoggingController.LogInfo("No PMC's will spawn during this raid");
            }
        }

        protected override IEnumerable<Vector3> GetSpawnPositionsForBotGroup(Models.BotSpawnInfo botGroup)
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            SpawnPointParams? spawnPoint = locationData.TryGetFurthestSpawnPointFromAllPlayers(getSpawnCategoryMask(), pendingSpawnPoints.ToArray());
            if (!spawnPoint.HasValue)
            {
                LoggingController.LogError("Could not find a valid spawn point for PMC group");
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

            if (ConfigController.Config.BotSpawns.MaxAliveBots.ContainsKey(locationID))
            {
                MaxAliveBots = ConfigController.Config.BotSpawns.MaxAliveBots[locationID];
            }
            LoggingController.LogInfo("Max PMC's on the map (" + locationID + ") at the same time: " + MaxAliveBots);
        }

        private Configuration.MinMaxConfig getPMCCount()
        {
            Components.LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>();

            // Determine how much to reduce the initial PMC's based on raid ET (used for Scav runs in Late to the Party)
            double playerCountFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayersVsRaidET, getRaidTimeRemainingFraction());
            playerCountFactor *= ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayers;

            // Choose the number of initial PMC's to spawn
            int pmcOffset = locationData.IsScavRun ? 0 : 1;
            int minPlayers = (int)Math.Floor((locationData.CurrentLocation.MinPlayers * playerCountFactor) - pmcOffset);
            int maxPlayers = (int)Math.Ceiling((locationData.CurrentLocation.MaxPlayers * playerCountFactor) - pmcOffset);
            
            return new Configuration.MinMaxConfig(minPlayers, maxPlayers);
        }

        private Func<Task> generateBotGroupsTask(BotDifficulty botdifficulty, int totalCount)
        {
            return async () =>
            {
                int botsGenerated = 0;

                try
                {
                    // Ensure the PMC-conversion chances have remained at 0%
                    ConfigController.AdjustPMCConversionChances(0, true);

                    LoggingController.LogInfo("Generating " + totalCount + " PMC's...");

                    // Spawn smaller PMC groups later in raids
                    double groupSizeFactor = ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PMCs.FractionOfMaxPlayersVsRaidET, getRaidTimeRemainingFraction());

                    System.Random random = new System.Random();
                    int botGroup = 1;
                    while (botsGenerated < totalCount)
                    {
                        // Determine how many bots to spawn in the group, but do not exceed the maximum number of bots allowed to spawn
                        int botsInGroup = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.BotSpawns.PMCs.BotsPerGroupDistribution, random.NextDouble()));
                        botsInGroup = (int)Math.Ceiling(botsInGroup * groupSizeFactor);
                        botsInGroup = (int)Math.Min(botsInGroup, totalCount - botsGenerated);

                        // Randomly select the PMC faction (BEAR or USEC) for all of the bots in the group
                        WildSpawnType spawnType = Helpers.BotBrainHelpers.pmcSpawnTypes.Random();

                        Models.BotSpawnInfo group = await GenerateBotGroup(spawnType, botdifficulty, botsInGroup);
                        BotGroups.Add(group);

                        botsGenerated += botsInGroup;
                        botGroup++;
                    }

                    LoggingController.LogInfo("Generating " + totalCount + " PMC's...done.");
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

                    hasGeneratedBotGroups = true;
                }
            };
        }

        private float getMinDistanceFromOtherPlayers()
        {
            if (getRaidTimeRemainingFraction() > 0.98)
            {
                return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersInitial;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<LocationData>().CurrentLocation.Name.ToLower().Contains("factory"))
            {
                return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersDuringRaidFactory;
            }

            return ConfigController.Config.BotSpawns.PMCs.MinDistanceFromPlayersDuringRaid;
        }

        private ESpawnCategoryMask getSpawnCategoryMask()
        {
            if (getRaidTimeRemainingFraction() > 0.98)
            {
                return ESpawnCategoryMask.Player;
            }

            return ESpawnCategoryMask.All;
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
