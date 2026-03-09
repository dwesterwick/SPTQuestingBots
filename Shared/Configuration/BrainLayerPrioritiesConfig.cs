using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BrainLayerPrioritiesConfig
    {
        [DataMember(Name = "questing")]
        public int Questing { get; set; } = 18;

        [DataMember(Name = "following")]
        public int Following { get; set; } = 19;

        [DataMember(Name = "regrouping")]
        public int Regrouping { get; set; } = 26;

        [DataMember(Name = "sleeping")]
        public int Sleeping { get; set; } = 99;

        public BrainLayerPrioritiesConfig()
        {

        }
    }
}
