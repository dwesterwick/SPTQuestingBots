using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestGenerationConfig
    {
        [DataMember(Name = "navmesh_search_distance_item", IsRequired = true)]
        public float NavMeshSearchDistanceItem { get; set; } = 2;

        [DataMember(Name = "navmesh_search_distance_zone", IsRequired = true)]
        public float NavMeshSearchDistanceZone { get; set; } = 2;

        [DataMember(Name = "navmesh_search_distance_spawn", IsRequired = true)]
        public float NavMeshSearchDistanceSpawn { get; set; } = 2;

        [DataMember(Name = "navmesh_search_distance_doors", IsRequired = true)]
        public float NavMeshSearchDistanceDoors { get; set; } = 1.5f;

        public QuestGenerationConfig()
        {

        }
    }
}
