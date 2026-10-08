using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LookAroundLimitsConfig
    {
        [DataMember(Name = "horizontal_deg", IsRequired = true)]
        public float HorizontalDeg { get; set; } = 90;

        [DataMember(Name = "vertical_down_deg", IsRequired = true)]
        public float VerticalDownDeg { get; set; } = 30;

        [DataMember(Name = "vertical_up_deg", IsRequired = true)]
        public float VerticalUpDeg { get; set; } = 30;

        [DataMember(Name = "direction_change_delay", IsRequired = true)]
        public MinMaxConfig DirectionChangeDelay { get; set; } = new MinMaxConfig(0.5, 5);

        [DataMember(Name = "direction_randomness", IsRequired = true)]
        public float DirectionRandomness { get; set; } = 30;

        [DataMember(Name = "max_rotation_speed", IsRequired = true)]
        public float MaxRotationSpeed { get; set; } = 120;

        public LookAroundLimitsConfig()
        {

        }
    }
}
