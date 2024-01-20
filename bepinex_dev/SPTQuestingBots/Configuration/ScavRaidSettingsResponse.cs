using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class ScavRaidSettingsResponse
    {
        [JsonProperty("maps")]
        public Dictionary<string, Configuration.ScavRaidSettingsConfig> Maps { get; set; } = new Dictionary<string, Configuration.ScavRaidSettingsConfig>();

        public ScavRaidSettingsResponse()
        {

        }
    }
}
