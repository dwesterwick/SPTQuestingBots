using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotPathingConfig
    {
        [JsonProperty("max_start_position_discrepancy")]
        public float MaxStartPositionDiscrepancy { get; set; } = 0.5f;

        [JsonProperty("incomplete_path_retry_interval")]
        public float IncompletePathRetryInterval { get; set; } = 5;

        public BotPathingConfig()
        {

        }
    }
}
