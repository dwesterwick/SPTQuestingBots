using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class AdjustPScavChanceConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("chance_vs_time_remaining_fraction")]
        public double[][] ChanceVsTimeRemainingFraction { get; set; } = new double[0][];

        public AdjustPScavChanceConfig()
        {

        }
    }
}
