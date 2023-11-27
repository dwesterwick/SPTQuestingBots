using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class QuestGenerationConfig
    {
        [JsonProperty("navmesh_search_distance_item")]
        public float NavMeshSearchDistanceItem { get; set; } = 2;

        [JsonProperty("navmesh_search_distance_zone")]
        public float NavMeshSearchDistanceZone { get; set; } = 2;

        [JsonProperty("navmesh_search_distance_spawn")]
        public float NavMeshSearchDistanceSpawn { get; set; } = 2;

        [JsonProperty("navmesh_search_distance_doors")]
        public float NavMeshSearchDistanceDoors { get; set; } = 1.5f;

        public QuestGenerationConfig()
        {

        }
    }
}
