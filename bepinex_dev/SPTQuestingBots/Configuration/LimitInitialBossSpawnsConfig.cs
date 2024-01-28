using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class LimitInitialBossSpawnsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("disable_rogue_delay")]
        public bool DisableRogueDelay { get; set; } = true;

        [JsonProperty("max_initial_bosses")]
        public int MaxInitialBosses { get; set; } = 10;

        [JsonProperty("max_initial_rogues")]
        public int MaxInitialRogues { get; set; } = 6;

        public LimitInitialBossSpawnsConfig()
        {

        }
    }
}
