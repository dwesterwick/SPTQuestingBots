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
        [DataMember(Name = "scav", IsRequired = true)]
        public bool Scav { get; set; } = true;

        [DataMember(Name = "pscav", IsRequired = true)]
        public bool PScav { get; set; } = true;

        [DataMember(Name = "pmc", IsRequired = true)]
        public bool PMC { get; set; } = true;

        [DataMember(Name = "boss", IsRequired = true)]
        public bool Boss { get; set; } = false;

        public BotTypeConfig()
        {

        }
    }
}
