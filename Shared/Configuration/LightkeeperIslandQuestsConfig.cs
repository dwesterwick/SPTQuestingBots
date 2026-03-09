using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LightkeeperIslandQuestsConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = false;

        public LightkeeperIslandQuestsConfig()
        {

        }
    }
}
