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
        [DataMember(Name = "enable_debounce_time")]
        public float EnableDebounceTime { get; set; } = 1;

        [DataMember(Name = "stamina")]
        public MinMaxConfig Stamina { get; set; } = new MinMaxConfig();

        [DataMember(Name = "sharp_path_corners")]
        public DistanceAngleConfig SharpPathCorners { get; set; } = new DistanceAngleConfig();

        [DataMember(Name = "approaching_closed_doors")]
        public DistanceAngleConfig ApproachingClosedDoors { get; set; } = new DistanceAngleConfig();

        public SprintingLimitationsConfig()
        {

        }
    }
}
