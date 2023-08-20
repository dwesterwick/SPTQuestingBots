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

        public StuckBotDetectionConfig()
        {

        }
    }
}
