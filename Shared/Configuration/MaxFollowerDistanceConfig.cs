using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class MaxFollowerDistanceConfig
    {
        [DataMember(Name = "max_wait_time", EmitDefaultValue = false, IsRequired = true)]
        public float MaxWaitTime { get; set; } = 5;

        [DataMember(Name = "min_regroup_time", EmitDefaultValue = false, IsRequired = true)]
        public float MinRegroupTime { get; set; } = 1;

        [DataMember(Name = "regroup_pause_time", EmitDefaultValue = false, IsRequired = true)]
        public float RegroupPauseTime { get; set; } = 2;

        [DataMember(Name = "target_position_variation_allowed", EmitDefaultValue = false, IsRequired = true)]
        public float TargetPositionVariationAllowed { get; set; } = 1;

        [DataMember(Name = "target_range_questing", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig TargetRangeQuesting { get; set; } = new MinMaxConfig();

        [DataMember(Name = "target_range_combat", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig TargetRangeCombat { get; set; } = new MinMaxConfig();

        [DataMember(Name = "nearest", EmitDefaultValue = false, IsRequired = true)]
        public float Nearest { get; set; } = 35;

        [DataMember(Name = "furthest", EmitDefaultValue = false, IsRequired = true)]
        public float Furthest { get; set; } = 50;

        public MaxFollowerDistanceConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
