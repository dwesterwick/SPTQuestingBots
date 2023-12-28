using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class ExtractionRequirementsConfig
    {
        [JsonProperty("must_extract_time_remaining")]
        public float MustExtractTimeRemaining { get; set; } = 300;

        [JsonProperty("total_quests")]
        public MinMaxConfig TotalQuests { get; set; } = new MinMaxConfig();

        [JsonProperty("EFT_quests")]
        public MinMaxConfig EFTQuests { get; set; } = new MinMaxConfig();

        public ExtractionRequirementsConfig()
        {

        }
    }
}
