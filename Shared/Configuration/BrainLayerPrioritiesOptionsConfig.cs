using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BrainLayerPrioritiesOptionsConfig
    {
        [DataMember(Name = "with_sain", IsRequired = true)]
        public BrainLayerPrioritiesConfig WithSAIN { get; set; } = new BrainLayerPrioritiesConfig();

        [DataMember(Name = "without_sain", IsRequired = true)]
        public BrainLayerPrioritiesConfig WithoutSAIN { get; set; } = new BrainLayerPrioritiesConfig();

        public BrainLayerPrioritiesOptionsConfig()
        {

        }
    }
}
