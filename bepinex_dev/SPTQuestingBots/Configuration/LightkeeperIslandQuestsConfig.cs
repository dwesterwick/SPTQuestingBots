using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class LightkeeperIslandQuestsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("min_sain_version")]
        public string MinSainVersion { get; set; } = "3.1.0.99";

        public LightkeeperIslandQuestsConfig()
        {

        }
    }
}
