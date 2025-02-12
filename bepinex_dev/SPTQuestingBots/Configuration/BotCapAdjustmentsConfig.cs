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
        [JsonProperty("use_EFT_bot_caps")]
        public bool UseEFTBotCaps { get; set; } = true;

        [JsonProperty("only_decrease_bot_caps")]
        public bool OnlyDecreaseBotCaps { get; set; } = true;

        [JsonProperty("map_specific_adjustments")]
        public Dictionary<string, int> MapSpecificAdjustments { get; set; } = new Dictionary<string, int>();

        public BotCapAdjustmentsConfig()
        {

        }
    }
}
