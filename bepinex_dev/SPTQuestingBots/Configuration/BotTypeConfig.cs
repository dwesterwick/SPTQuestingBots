using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotTypeConfig
    {
        [JsonProperty("scav")]
        public bool Scav { get; set; } = false;

        [JsonProperty("pscav")]
        public bool PScav { get; set; } = false;

        [JsonProperty("pmc")]
        public bool PMC { get; set; } = true;

        [JsonProperty("boss")]
        public bool Boss { get; set; } = false;

        public BotTypeConfig()
        {

        }
    }
}
