using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotCapAdjustmentsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("min_other_bots_allowed_to_spawn")]
        public int MinOtherBotsAllowedToSpawn { get; set; } = 4;

        [JsonProperty("add_max_players_to_bot_cap")]
        public bool AddMaxPlayersToBotCap { get; set; } = false;

        [JsonProperty("max_additional_bots")]
        public int MaxAdditionalBots { get; set; } = 10;

        [JsonProperty("max_total_bots")]
        public int MaxTotalBots { get; set; } = 40;

        public BotCapAdjustmentsConfig()
        {

        }
    }
}
