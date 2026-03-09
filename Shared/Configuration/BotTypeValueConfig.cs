using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotTypeValueConfig
    {
        [DataMember(Name = "scav")]
        public float Scav { get; set; } = 0;

        [DataMember(Name = "pscav")]
        public float PScav { get; set; } = 0;

        [DataMember(Name = "pmc")]
        public float PMC { get; set; } = 0;

        [DataMember(Name = "boss")]
        public float Boss { get; set; } = 0;

        public BotTypeValueConfig()
        {
        }
    }
}
