using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class AdjustPScavChanceConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "chance_vs_time_remaining_fraction", IsRequired = true)]
        public double[][] ChanceVsTimeRemainingFraction { get; set; } = Array.Empty<double[]>();

        public AdjustPScavChanceConfig()
        {

        }
    }
}
