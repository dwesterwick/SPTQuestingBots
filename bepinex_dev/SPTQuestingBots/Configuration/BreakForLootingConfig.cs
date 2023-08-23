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

        [JsonProperty("min_time_between_looting")]
        public float MinTimeBetweenLooting { get; set; } = 10;

        [JsonProperty("max_distance_to_loot")]
        public float MaxDistanceToLoot { get; set; } = 5;

        [JsonProperty("max_time_to_start_looting")]
        public float MaxTimeToStartLooting { get; set; } = 2;

        [JsonProperty("max_looting_time")]
        public float MaxLootingTime { get; set; } = 30;

        public BreakForLootingConfig()
        {

        }
    }
}
