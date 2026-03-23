using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class SprintingLimitationsConfig
    {
        [DataMember(Name = "enable_debounce_time", EmitDefaultValue = false, IsRequired = true)]
        public float EnableDebounceTime { get; set; } = 1;

        [DataMember(Name = "stamina", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig Stamina { get; set; } = new MinMaxConfig();

        [DataMember(Name = "sharp_path_corners", EmitDefaultValue = false, IsRequired = true)]
        public DistanceAngleConfig SharpPathCorners { get; set; } = new DistanceAngleConfig();

        [DataMember(Name = "approaching_closed_doors", EmitDefaultValue = false, IsRequired = true)]
        public DistanceAngleConfig ApproachingClosedDoors { get; set; } = new DistanceAngleConfig();

        public SprintingLimitationsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
