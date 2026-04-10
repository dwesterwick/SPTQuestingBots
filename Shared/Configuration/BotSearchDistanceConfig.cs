using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotSearchDistanceConfig
    {
        [DataMember(Name = "objective_reached_ideal", IsRequired = true)]
        public float OjectiveReachedIdeal { get; set; } = 3;

        [DataMember(Name = "objective_reached_navmesh_path_error", IsRequired = true)]
        public float ObjectiveReachedNavMeshPathError { get; set; } = 20;

        [DataMember(Name = "max_navmesh_path_error", IsRequired = true)]
        public float MaxNavMeshPathError { get; set; } = 10;

        public BotSearchDistanceConfig()
        {

        }
    }
}
