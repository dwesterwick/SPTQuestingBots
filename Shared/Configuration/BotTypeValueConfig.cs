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
        [DataMember(Name = "scav", EmitDefaultValue = false, IsRequired = true)]
        public float Scav { get; set; } = 0;

        [DataMember(Name = "pscav", EmitDefaultValue = false, IsRequired = true)]
        public float PScav { get; set; } = 0;

        [DataMember(Name = "pmc", EmitDefaultValue = false, IsRequired = true)]
        public float PMC { get; set; } = 0;

        [DataMember(Name = "boss", EmitDefaultValue = false, IsRequired = true)]
        public float Boss { get; set; } = 0;

        public BotTypeValueConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
