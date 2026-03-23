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
        [DataMember(Name = "reduceLootByPercent", IsRequired = true)]
        public bool ReduceLootByPercent { get; set; } = true;

        [DataMember(Name = "minDynamicLootPercent", IsRequired = true)]
        public float MinDynamicLootPercent { get; set; } = 50;

        [DataMember(Name = "minStaticLootPercent", IsRequired = true)]
        public float MinStaticLootPercent { get; set; } = 40;

        [DataMember(Name = "reducedChancePercent", IsRequired = true)]
        public float ReducedChancePercent { get; set; } = 95;

        [DataMember(Name = "reductionPercentWeights", IsRequired = true)]
        public Dictionary<string, int> ReductionPercentWeights { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "adjustWaves", IsRequired = true)]
        public bool AdjustWaves { get; set; } = true;

        public ScavRaidSettingsConfig()
        {

        }
    }
}
