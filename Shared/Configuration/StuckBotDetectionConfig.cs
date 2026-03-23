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
        [DataMember(Name = "distance", IsRequired = true)]
        public float Distance { get; set; } = 2;

        [DataMember(Name = "time", IsRequired = true)]
        public float Time { get; set; } = 20;

        [DataMember (Name = "max_count", IsRequired = true)]
        public int MaxCount { get; set; } = 10;

        [DataMember(Name = "follower_break_time", IsRequired = true)]
        public float FollowerBreakTime { get; set; } = 10;

        [DataMember(Name = "max_not_able_bodied_time", IsRequired = true)]
        public float MaxNotAbleBodiedTime { get; set; } = 120;

        [DataMember(Name = "stuck_bot_remedies", IsRequired = true)]
        public StuckBotRemediesConfig StuckBotRemedies { get; set; } = new StuckBotRemediesConfig();

        public StuckBotDetectionConfig()
        {

        }
    }
}
