using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BrainLayerPrioritiesConfig
    {
        [JsonProperty("questing")]
        public int Questing { get; set; } = 18;

        [JsonProperty("following")]
        public int Following { get; set; } = 19;

        [JsonProperty("regrouping")]
        public int Regrouping { get; set; } = 26;

        [JsonProperty("sleeping")]
        public int Sleeping { get; set; } = 99;

        public BrainLayerPrioritiesConfig()
        {

        }
    }
}
