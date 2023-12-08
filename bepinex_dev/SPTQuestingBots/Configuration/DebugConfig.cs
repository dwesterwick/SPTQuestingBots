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

        [JsonProperty("scav_cooldown_time")]
        public long ScavCooldownTime { get; set; } = 1500;

        [JsonProperty("full_length_scav_raids")]
        public bool FullLengthScavRaids { get; set; } = false;

        [JsonProperty("free_labs_access")]
        public bool FreeLabsAccess { get; set; } = false;

        [JsonProperty("always_spawn_pmcs")]
        public bool AlwaysSpawnPMCs { get; set; } = false;

        [JsonProperty("show_zone_outlines")]
        public bool ShowZoneOutlines { get; set; } = false;

        [JsonProperty("show_failed_paths")]
        public bool ShowFailedPaths { get; set; } = false;

        [JsonProperty("show_door_interaction_test_points")]
        public bool ShowDoorInteractionTestPoints { get; set; } = false;

        public DebugConfig()
        {

        }
    }
}
