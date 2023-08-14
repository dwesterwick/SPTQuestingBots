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

        [JsonProperty("max_raid_ET")]
        public float MaxRaidET { get; set; } = 30;

        public InitialPMCSpawnsConfig()
        {

        }
    }
}
