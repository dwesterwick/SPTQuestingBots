using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class ScavRaidSettingsConfig
    {
        [JsonProperty("reduceLootByPercent")]
        public bool ReduceLootByPercent { get; set; } = true;

        [JsonProperty("minDynamicLootPercent")]
        public float MinDynamicLootPercent { get; set; } = 50;

        [JsonProperty("minStaticLootPercent")]
        public float MinStaticLootPercent { get; set; } = 40;

        [JsonProperty("reducedChancePercent")]
        public float ReducedChancePercent { get; set; } = 95;

        [JsonProperty("reductionPercentWeights")]
        public Dictionary<string, int> ReductionPercentWeights { get; set; } = new Dictionary<string, int>();

        [JsonProperty("adjustWaves")]
        public bool AdjustWaves { get; set; } = true;

        public ScavRaidSettingsConfig()
        {

        }
    }
}
