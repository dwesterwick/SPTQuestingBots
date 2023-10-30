using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BreakForLootingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("min_time_between_looting_checks")]
        public float MinTimeBetweenLootingChecks { get; set; } = 50;

        [JsonProperty("min_time_between_follower_looting_checks")]
        public float MinTimeBetweenFollowerLootingChecks { get; set; } = 30;

        [JsonProperty("min_time_between_looting_events")]
        public float MinTimeBetweenLootingEvents { get; set; } = 80;

        [JsonProperty("max_time_to_start_looting")]
        public float MaxTimeToStartLooting { get; set; } = 2;

        [JsonProperty("max_loot_scan_time")]
        public float MaxLootScanTime { get; set; } = 4;

        [JsonProperty("max_distance_from_boss")]
        public float MaxDistanceFromBoss { get; set; } = 75;

        public BreakForLootingConfig()
        {

        }
    }
}
