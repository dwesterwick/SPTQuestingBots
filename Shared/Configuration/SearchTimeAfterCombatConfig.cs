using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class SearchTimeAfterCombatConfig
    {
        [DataMember(Name = "prioritized_sain", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig PrioritizedSAIN { get; set; } = new MinMaxConfig();

        [DataMember(Name = "prioritized_questing", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig PrioritizedQuesting { get; set; } = new MinMaxConfig();

        public SearchTimeAfterCombatConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
