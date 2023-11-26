using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class OpenInitiallyLockedDoorsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("search_radius")]
        public float SearchRadius { get; set; } = 2;

        public OpenInitiallyLockedDoorsConfig()
        {

        }
    }
}
