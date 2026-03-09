using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ScavRaidSettingsResponse
    {
        [DataMember(Name = "maps")]
        public Dictionary<string, Configuration.ScavRaidSettingsConfig> Maps { get; set; } = new Dictionary<string, Configuration.ScavRaidSettingsConfig>();

        public ScavRaidSettingsResponse()
        {

        }
    }
}
