using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class DebugConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("scav_cooldown_time")]
        public long ScavCooldownTime { get; set; } = 1500;

        [JsonProperty("show_zone_outlines")]
        public bool ShowZoneOutlines { get; set; } = true;

        public DebugConfig()
        {

        }
    }
}
