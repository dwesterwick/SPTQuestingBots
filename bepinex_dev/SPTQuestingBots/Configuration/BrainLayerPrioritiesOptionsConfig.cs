using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BrainLayerPrioritiesOptionsConfig
    {
        [JsonProperty("with_sain")]
        public BrainLayerPrioritiesConfig WithSAIN { get; set; } = new BrainLayerPrioritiesConfig();

        [JsonProperty("without_sain")]
        public BrainLayerPrioritiesConfig WithoutSAIN { get; set; } = new BrainLayerPrioritiesConfig();

        public BrainLayerPrioritiesOptionsConfig()
        {

        }
    }
}
