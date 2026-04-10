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
        [DataMember(Name = "questing", IsRequired = true)]
        public int Questing { get; set; } = 18;

        [DataMember(Name = "following", IsRequired = true)]
        public int Following { get; set; } = 19;

        [DataMember(Name = "regrouping", IsRequired = true)]
        public int Regrouping { get; set; } = 26;

        [DataMember(Name = "sleeping", IsRequired = true)]
        public int Sleeping { get; set; } = 99;

        public BrainLayerPrioritiesConfig()
        {

        }
    }
}
