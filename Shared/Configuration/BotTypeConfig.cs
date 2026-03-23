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
        [DataMember(Name = "scav", EmitDefaultValue = false, IsRequired = true)]
        public bool Scav { get; set; } = false;

        [DataMember(Name = "pscav", EmitDefaultValue = false, IsRequired = true)]
        public bool PScav { get; set; } = false;

        [DataMember(Name = "pmc", EmitDefaultValue = false, IsRequired = true)]
        public bool PMC { get; set; } = true;

        [DataMember(Name = "boss", EmitDefaultValue = false, IsRequired = true)]
        public bool Boss { get; set; } = false;

        public BotTypeConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
