using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestSelectionConfig
    {
        [DataMember(Name = "max_calc_time_per_frame_ms", IsRequired = true)]
        public float MaxCalcTimePerFrame { get; set; } = 1;

        [DataMember(Name = "timeout", IsRequired = true)]
        public float Timeout { get; set; } = 20000;

        public QuestSelectionConfig()
        {

        }
    }
}
