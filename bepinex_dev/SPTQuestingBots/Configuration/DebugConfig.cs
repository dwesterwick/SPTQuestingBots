using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class DebugConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("always_spawn_pmcs")]
        public bool AlwaysSpawnPMCs { get; set; } = false;

        [JsonProperty("always_spawn_pscavs")]
        public bool AlwaysSpawnPScavs { get; set; } = false;

        [JsonProperty("show_zone_outlines")]
        public bool ShowZoneOutlines { get; set; } = false;

        [JsonProperty("show_failed_paths")]
        public bool ShowFailedPaths { get; set; } = false;

        [JsonProperty("show_door_interaction_test_points")]
        public bool ShowDoorInteractionTestPoints { get; set; } = false;

        [JsonProperty("allow_zero_distance_sleeping")]
        public bool AllowZeroDistanceSleeping { get; set; } = false;

        public DebugConfig()
        {

        }
    }
}
