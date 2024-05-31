using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class AdvancedEFTBotCountManagementConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("use_EFT_bot_caps")]
        public bool UseEFTBotCaps { get; set; } = true;

        [JsonProperty("only_decrease_bot_caps")]
        public bool OnlyDecreaseBotCaps { get; set; } = true;

        [JsonProperty("bot_cap_adjustments")]
        public Dictionary<string, int> BotCapAdjustments { get; set; } = new Dictionary<string, int>();

        public AdvancedEFTBotCountManagementConfig()
        {

        }
    }
}
