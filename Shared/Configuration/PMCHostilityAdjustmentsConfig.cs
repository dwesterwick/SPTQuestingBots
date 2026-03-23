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
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "pmcs_always_hostile_against_pmcs", IsRequired = true)]
        public bool PMCsAlwaysHostileAgainstPMCs { get; set; } = true;

        [DataMember(Name = "pmcs_always_hostile_against_scavs", IsRequired = true)]
        public bool PMCsAlwaysHostileAgainstScavs { get; set; } = true;

        [DataMember(Name = "global_scav_enemy_chance", IsRequired = true)]
        public int GlobalScavEnemyChance { get; set; } = 100;

        [DataMember(Name = "pmc_enemy_roles", IsRequired = true)]
        public string[] PMCEnemyRoles { get; set; } = Array.Empty<string>();

        public PMCHostilityAdjustmentsConfig()
        {

        }
    }
}
