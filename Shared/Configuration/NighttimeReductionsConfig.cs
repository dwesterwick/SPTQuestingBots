using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class NighttimeReductionsConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "chance_percentage", IsRequired = true)]
        public int ChancePercentage { get; set; } = 75;

        [DataMember(Name = "size_percentage", IsRequired = true)]
        public int SizePercentage { get; set; } = 40;

        public NighttimeReductionsConfig()
        {

        }
    }
}
