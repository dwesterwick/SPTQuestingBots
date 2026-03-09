using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotTypeConfig
    {
        [DataMember(Name = "scav")]
        public bool Scav { get; set; } = false;

        [DataMember(Name = "pscav")]
        public bool PScav { get; set; } = false;

        [DataMember(Name = "pmc")]
        public bool PMC { get; set; } = true;

        [DataMember(Name = "boss")]
        public bool Boss { get; set; } = false;

        public BotTypeConfig()
        {

        }
    }
}
