using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class SearchTimeAfterCombatConfig
    {
        [JsonProperty("prioritized_sain")]
        public MinMaxConfig PrioritizedSAIN { get; set; } = new MinMaxConfig();

        [JsonProperty("prioritized_questing")]
        public MinMaxConfig PrioritizedQuesting { get; set; } = new MinMaxConfig();

        public SearchTimeAfterCombatConfig()
        {

        }
    }
}
