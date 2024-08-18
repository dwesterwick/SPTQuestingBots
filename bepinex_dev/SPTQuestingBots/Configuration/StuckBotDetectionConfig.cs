using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class StuckBotDetectionConfig
    {
        [JsonProperty("distance")]
        public float Distance { get; set; } = 2;

        [JsonProperty("time")]
        public float Time { get; set; } = 20;

        [JsonProperty("max_count")]
        public int MaxCount { get; set; } = 10;

        [JsonProperty("follower_break_time")]
        public float FollowerBreakTime { get; set; } = 10;

        [JsonProperty("max_not_able_bodied_time")]
        public float MaxNotAbleBodiedTime { get; set; } = 120;

        [JsonProperty("stuck_bot_remedies")]
        public StuckBotRemediesConfig StuckBotRemedies { get; set; } = new StuckBotRemediesConfig();

        public StuckBotDetectionConfig()
        {

        }
    }
}
