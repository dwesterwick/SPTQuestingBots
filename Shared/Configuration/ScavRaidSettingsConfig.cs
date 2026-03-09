using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ScavRaidSettingsConfig
    {
        [DataMember(Name = "reduceLootByPercent")]
        public bool ReduceLootByPercent { get; set; } = true;

        [DataMember(Name = "minDynamicLootPercent")]
        public float MinDynamicLootPercent { get; set; } = 50;

        [DataMember(Name = "minStaticLootPercent")]
        public float MinStaticLootPercent { get; set; } = 40;

        [DataMember(Name = "reducedChancePercent")]
        public float ReducedChancePercent { get; set; } = 95;

        [DataMember(Name = "reductionPercentWeights")]
        public Dictionary<string, int> ReductionPercentWeights { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "adjustWaves")]
        public bool AdjustWaves { get; set; } = true;

        public ScavRaidSettingsConfig()
        {

        }
    }
}
