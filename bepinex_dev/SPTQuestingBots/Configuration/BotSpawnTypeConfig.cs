using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotSpawnTypeConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("min_raid_time_remaining")]
        public float MinRaidTimeRemaining { get; set; } = 0;

        [JsonProperty("min_distance_from_players_initial")]
        public float MinDistanceFromPlayersInitial { get; set; } = 25;

        [JsonProperty("min_distance_from_players_during_raid")]
        public float MinDistanceFromPlayersDuringRaid { get; set; } = 100;

        [JsonProperty("min_distance_from_players_during_raid_factory")]
        public float MinDistanceFromPlayersDuringRaidFactory { get; set; } = 50;

        [JsonProperty("fraction_of_max_players")]
        public float FractionOfMaxPlayers { get; set; } = 1;

        [JsonProperty("time_randomness")]
        public float TimeRandomness { get; set; } = 0;

        [JsonProperty("fraction_of_max_players_vs_raidET")]
        public double[][] FractionOfMaxPlayersVsRaidET { get; set; } = new double[0][];

        [JsonProperty("bots_per_group_distribution")]
        public double[][] BotsPerGroupDistribution { get; set; } = new double[0][];

        [JsonProperty("bot_difficulty_as_online")]
        public double[][] BotDifficultyAsOnline { get; set; } = new double[0][];

        public BotSpawnTypeConfig()
        {

        }
    }
}
