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
        [JsonProperty("with_sain")]
        public MinMaxConfig WithSAIN { get; set; } = new MinMaxConfig();

        [JsonProperty("without_sain")]
        public MinMaxConfig WithoutSAIN { get; set; } = new MinMaxConfig();

        public SearchTimeAfterCombatConfig()
        {

        }
    }
}
