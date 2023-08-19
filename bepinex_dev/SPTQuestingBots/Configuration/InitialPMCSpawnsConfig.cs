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

        [JsonProperty("pmcs_vs_raidET")]
        public double[][] PMCsVsRaidET { get; set; } = new double[0][];

        [JsonProperty("conversion_factor_after_initial_spawns")]
        public float ConversionFactorAfterInitialSpawns { get; set; } = 0.1f;

        public InitialPMCSpawnsConfig()
        {

        }
    }
}
