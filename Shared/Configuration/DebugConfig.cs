using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class DebugConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "always_spawn_pmcs")]
        public bool AlwaysSpawnPMCs { get; set; } = false;

        [DataMember(Name = "always_spawn_pscavs")]
        public bool AlwaysSpawnPScavs { get; set; } = false;

        [DataMember(Name = "show_zone_outlines")]
        public bool ShowZoneOutlines { get; set; } = false;

        [DataMember(Name = "show_failed_paths")]
        public bool ShowFailedPaths { get; set; } = false;

        [DataMember(Name = "show_door_interaction_test_points")]
        public bool ShowDoorInteractionTestPoints { get; set; } = false;

        [DataMember(Name = "allow_zero_distance_sleeping")]
        public bool AllowZeroDistanceSleeping { get; set; } = false;

        public DebugConfig()
        {

        }
    }
}
