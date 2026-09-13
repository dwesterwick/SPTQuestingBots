using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class EftLocalAvoidanceConfig
    {
        [DataMember(Name = "allow_for_questing_bots", IsRequired = true)]
        public bool AllowForQuestingBots { get; set; } = true;

        [DataMember(Name = "avoidance_radius", IsRequired = true)]
        public MinMaxConfig AvoidanceRadius { get; set; } = new MinMaxConfig(0.75, 1);

        [DataMember(Name = "radius_multiplier_to_drop_offset", IsRequired = true)]
        public float RadiusMultiplierToDropOffset { get; set; } = 2;

        public EftLocalAvoidanceConfig()
        {

        }
    }
}
