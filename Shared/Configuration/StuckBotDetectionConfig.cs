using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class StuckBotDetectionConfig
    {
        [DataMember(Name = "distance")]
        public float Distance { get; set; } = 2;

        [DataMember(Name = "time")]
        public float Time { get; set; } = 20;

        [DataMember(Name = "max_count")]
        public int MaxCount { get; set; } = 10;

        [DataMember(Name = "follower_break_time")]
        public float FollowerBreakTime { get; set; } = 10;

        [DataMember(Name = "max_not_able_bodied_time")]
        public float MaxNotAbleBodiedTime { get; set; } = 120;

        [DataMember(Name = "stuck_bot_remedies")]
        public StuckBotRemediesConfig StuckBotRemedies { get; set; } = new StuckBotRemediesConfig();

        public StuckBotDetectionConfig()
        {

        }
    }
}
