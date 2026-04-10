using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ExtractionRequirementsConfig
    {
        [DataMember(Name = "use_sain_for_extracting", IsRequired = true)]
        public bool UseSAINForExtracting { get; set; } = false;

        [DataMember(Name = "min_alive_time", IsRequired = true)]
        public float MinAliveTime { get; set; } = 60;

        [DataMember(Name = "must_extract_time_remaining", IsRequired = true)]
        public float MustExtractTimeRemaining { get; set; } = 300;

        [DataMember(Name = "total_quests", IsRequired = true)]
        public MinMaxConfig TotalQuests { get; set; } = new MinMaxConfig();

        [DataMember(Name = "EFT_quests", IsRequired = true)]
        public MinMaxConfig EFTQuests { get; set; } = new MinMaxConfig();

        public ExtractionRequirementsConfig()
        {

        }
    }
}
