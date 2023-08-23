using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotQuestingRequirementsConfig
    {
        [JsonProperty("min_hydration")]
        public float MinHydration { get; set; } = 50;

        [JsonProperty("min_energy")]
        public float MinEnergy { get; set; } = 50;

        [JsonProperty("min_health_head")]
        public float MinHealthHead { get; set; } = 50;

        [JsonProperty("min_health_chest")]
        public float MinHealthChest { get; set; } = 50;

        [JsonProperty("min_health_stomach")]
        public float MinHealthStomach { get; set; } = 50;

        [JsonProperty("min_health_legs")]
        public float MinHealthLegs { get; set; } = 50;

        [JsonProperty("max_overweight_percentage")]
        public float MaxOverweightPercentage { get; set; } = 100;

        [JsonProperty("break_for_looting")]
        public BreakForLootingConfig BreakForLooting { get; set; } = new BreakForLootingConfig();

        public BotQuestingRequirementsConfig()
        {

        }
    }
}
