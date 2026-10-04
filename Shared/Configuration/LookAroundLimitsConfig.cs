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
        public float HorizontalDeg { get; set; } = 100;

        [DataMember(Name = "vertical_down_deg", IsRequired = true)]
        public float VerticalDownDeg { get; set; } = 100;

        [DataMember(Name = "vertical_up_deg", IsRequired = true)]
        public float VerticalUpDeg { get; set; } = 100;

        [DataMember(Name = "direction_change_delay", IsRequired = true)]
        public MinMaxConfig DirectionChangeDelay { get; set; } = new MinMaxConfig(0.5, 5);

        public LookAroundLimitsConfig()
        {

        }
    }
}
