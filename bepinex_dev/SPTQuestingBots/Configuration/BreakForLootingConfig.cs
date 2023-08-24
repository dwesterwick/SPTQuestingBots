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

        [JsonProperty("check_proximity_to_loot")]
        public bool CheckProximityToLoot { get; set; } = true;

        [JsonProperty("loot_must_be_visible")]
        public bool LootMustBeVisible { get; set; } = true;

        [JsonProperty("min_time_between_looting_checks")]
        public float MinTimeBetweenLootingChecks { get; set; } = 5;

        [JsonProperty("min_time_between_looting_events")]
        public float MinTimeBetweenLootingEvents { get; set; } = 30;

        [JsonProperty("max_distance_to_loot")]
        public float MaxDistanceToLoot { get; set; } = 5;

        [JsonProperty("max_time_to_start_looting")]
        public float MaxTimeToStartLooting { get; set; } = 2;

        [JsonProperty("max_loot_scan_time")]
        public float MaxLootScanTime { get; set; } = 30;

        public BreakForLootingConfig()
        {

        }
    }
}
