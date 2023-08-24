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
        [JsonProperty("nearest")]
        public float Nearest { get; set; } = 20;

        [JsonProperty("furthest")]
        public float Furthest { get; set; } = 40;

        public MaxFollowerDistanceConfig()
        {

        }
    }
}
