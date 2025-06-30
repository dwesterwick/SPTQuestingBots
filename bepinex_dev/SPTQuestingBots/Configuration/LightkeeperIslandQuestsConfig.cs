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

        public LightkeeperIslandQuestsConfig()
        {

        }
    }
}
