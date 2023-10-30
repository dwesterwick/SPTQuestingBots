using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class MaxFollowerDistanceConfig
    {
        [JsonProperty("max_wait_time")]
        public float MaxWaitTime { get; set; } = 10;

        [JsonProperty("target_range")]
        public MinMaxConfig TargetRange { get; set; } = new MinMaxConfig();

        [JsonProperty("nearest")]
        public float Nearest { get; set; } = 35;

        [JsonProperty("furthest")]
        public float Furthest { get; set; } = 50;

        public MaxFollowerDistanceConfig()
        {

        }
    }
}
