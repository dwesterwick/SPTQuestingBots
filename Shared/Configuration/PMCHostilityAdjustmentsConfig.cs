using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class PMCHostilityAdjustmentsConfig
    {
        [DataMember(Name = "enabled", EmitDefaultValue = false, IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "pmcs_always_hostile_against_pmcs", EmitDefaultValue = false, IsRequired = true)]
        public bool PMCsAlwaysHostileAgainstPMCs { get; set; } = true;

        [DataMember(Name = "pmcs_always_hostile_against_scavs", EmitDefaultValue = false, IsRequired = true)]
        public bool PMCsAlwaysHostileAgainstScavs { get; set; } = true;

        [DataMember(Name = "global_scav_enemy_chance", EmitDefaultValue = false, IsRequired = true)]
        public int GlobalScavEnemyChance { get; set; } = 100;

        [DataMember(Name = "pmc_enemy_roles", EmitDefaultValue = false, IsRequired = true)]
        public string[] PMCEnemyRoles { get; set; } = Array.Empty<string>();

        public PMCHostilityAdjustmentsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
