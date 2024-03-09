using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class HearingSensorConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("max_distance_footsteps")]
        public float MaxDistanceFootsteps { get; set; } = 20;

        [JsonProperty("max_distance_gunfire")]
        public float MaxDistanceGunfire { get; set; } = 75;

        [JsonProperty("max_distance_gunfire_suppressed")]
        public float MaxDistanceGunfireSuppressed { get; set; } = 75;

        [JsonProperty("suspicious_time")]
        public MinMaxConfig SuspiciousTime { get; set; } = new MinMaxConfig();

        public HearingSensorConfig()
        {

        }
    }
}
