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
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "always_spawn_pmcs", IsRequired = true)]
        public bool AlwaysSpawnPMCs { get; set; } = false;

        [DataMember(Name = "always_spawn_pscavs", IsRequired = true)]
        public bool AlwaysSpawnPScavs { get; set; } = false;

        [DataMember(Name = "show_zone_outlines", IsRequired = true)]
        public bool ShowZoneOutlines { get; set; } = false;

        [DataMember(Name = "show_failed_paths", IsRequired = true)]
        public bool ShowFailedPaths { get; set; } = false;

        [DataMember(Name = "show_door_interaction_test_points", IsRequired = true)]
        public bool ShowDoorInteractionTestPoints { get; set; } = false;

        [DataMember(Name = "allow_zero_distance_sleeping", IsRequired = true)]
        public bool AllowZeroDistanceSleeping { get; set; } = false;

        public DebugConfig()
        {

        }
    }
}
