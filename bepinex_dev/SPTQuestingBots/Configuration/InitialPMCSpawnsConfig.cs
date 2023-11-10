using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class InitialPMCSpawnsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("blacklisted_pmc_bot_brains")]
        public string[] BlacklistedPMCBotBrains { get; set; } = new string[0];

        [JsonProperty("min_distance_from_players_initial")]
        public float MinDistanceFromPlayersInitial { get; set; } = 25;

        [JsonProperty("min_distance_from_players_during_raid")]
        public float MinDistanceFromPlayersDuringRaid { get; set; } = 100;

        [JsonProperty("min_distance_from_players_during_raid_factory")]
        public float MinDistanceFromPlayersDuringRaidFactory { get; set; } = 50;

        [JsonProperty("max_alive_initial_pmcs")]
        public Dictionary<string, int> MaxAliveInitialPMCs { get; set; } = new Dictionary<string, int>();

        [JsonProperty("initial_pmcs_vs_raidET")]
        public double[][] InitialPMCsVsRaidET { get; set; } = new double[0][];

        [JsonProperty("bots_per_group_distribution")]
        public double[][] BotsPerGroupDistribution { get; set; } = new double[0][];

        [JsonProperty("spawn_retry_time")]
        public float SpawnRetryTime { get; set; } = 10;

        [JsonProperty("min_other_bots_allowed_to_spawn")]
        public int MinOtherBotsAllowedToSpawn { get; set; } = 4;

        [JsonProperty("max_initial_bosses")]
        public int MaxInitialBosses { get; set; } = 10;

        [JsonProperty("max_initial_rogues")]
        public int MaxInitialRogues { get; set; } = 6;

        [JsonProperty("add_max_players_to_bot_cap")]
        public bool AddMaxPlayersToBotCap { get; set; } = false;

        [JsonProperty("max_additional_bots")]
        public int MaxAdditionalBots { get; set; } = 10;

        [JsonProperty("max_total_bots")]
        public int MaxTotalBots { get; set; } = 40;

        public InitialPMCSpawnsConfig()
        {

        }
    }
}
