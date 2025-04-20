using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class SprintingLimitationsConfig
    {
        [JsonProperty("enable_debounce_time")]
        public float EnableDebounceTime { get; set; } = 1;

        [JsonProperty("stamina")]
        public MinMaxConfig Stamina { get; set; } = new MinMaxConfig();

        [JsonProperty("sharp_path_corners")]
        public DistanceAngleConfig SharpPathCorners { get; set; } = new DistanceAngleConfig();

        [JsonProperty("approaching_closed_doors")]
        public DistanceAngleConfig ApproachingClosedDoors { get; set; } = new DistanceAngleConfig();

        public SprintingLimitationsConfig()
        {

        }
    }
}
