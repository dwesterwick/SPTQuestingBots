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

        [JsonProperty("conversion_factor_before_initial_spawns")]
        public float ConversionFactorBeforeInitialSpawns { get; set; } = 999;

        [JsonProperty("conversion_factor_after_initial_spawns")]
        public float ConversionFactorAfterInitialSpawns { get; set; } = 0.1f;

        [JsonProperty("max_raid_ET")]
        public float MaxRaidET { get; set; } = 30;

        public InitialPMCSpawnsConfig()
        {

        }
    }
}
