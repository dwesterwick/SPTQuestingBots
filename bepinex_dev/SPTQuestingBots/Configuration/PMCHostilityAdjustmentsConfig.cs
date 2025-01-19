using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class PMCHostilityAdjustmentsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("pmcs_always_hostile_against_pmcs")]
        public bool PMCsAlwaysHostileAgainstPMCs { get; set; } = true;

        [JsonProperty("pmcs_always_hostile_against_scavs")]
        public bool PMCsAlwaysHostileAgainstScavs { get; set; } = true;

        [JsonProperty("global_scav_enemy_chance")]
        public int GlobalScavEnemyChance { get; set; } = 100;

        [JsonProperty("pmc_enemy_roles")]
        public string[] PMCEnemyRoles { get; set; } = new string[0];

        public PMCHostilityAdjustmentsConfig()
        {

        }
    }
}
