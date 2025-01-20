using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotTypeValueConfig
    {
        [JsonProperty("scav")]
        public float Scav { get; set; } = 0;

        [JsonProperty("pscav")]
        public float PScav { get; set; } = 0;

        [JsonProperty("pmc")]
        public float PMC { get; set; } = 0;

        [JsonProperty("boss")]
        public float Boss { get; set; } = 0;

        public BotTypeValueConfig()
        {
        }
    }
}
