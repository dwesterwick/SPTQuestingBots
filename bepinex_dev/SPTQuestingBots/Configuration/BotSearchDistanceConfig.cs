using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotSearchDistanceConfig
    {
        [JsonProperty("objective_reached_ideal")]
        public float OjectiveReachedIdeal { get; set; } = 3;

        [JsonProperty("objective_reached_navmesh_path_error")]
        public float ObjectiveReachedNavMeshPathError { get; set; } = 20;

        [JsonProperty("max_navmesh_path_error")]
        public float MaxNavMeshPathError { get; set; } = 10;

        public BotSearchDistanceConfig()
        {

        }
    }
}
