using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotCapAdjustmentsConfig
    {
        [DataMember(Name = "use_EFT_bot_caps")]
        public bool UseEFTBotCaps { get; set; } = true;

        [DataMember(Name = "only_decrease_bot_caps")]
        public bool OnlyDecreaseBotCaps { get; set; } = true;

        [DataMember(Name = "map_specific_adjustments")]
        public Dictionary<string, int> MapSpecificAdjustments { get; set; } = new Dictionary<string, int>();

        public BotCapAdjustmentsConfig()
        {

        }
    }
}
