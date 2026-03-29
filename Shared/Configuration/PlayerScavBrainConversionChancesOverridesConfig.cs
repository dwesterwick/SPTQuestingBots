using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class PlayerScavBrainConversionChancesOverridesConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "chances", IsRequired = true)]
        public Dictionary<string, int> Chances { get; set; } = new Dictionary<string, int>();

        public PlayerScavBrainConversionChancesOverridesConfig()
        {

        }
    }
}
