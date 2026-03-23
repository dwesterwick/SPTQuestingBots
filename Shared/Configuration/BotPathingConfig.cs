using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotPathingConfig
    {
        [DataMember(Name = "max_start_position_discrepancy", IsRequired = true)]
        public float MaxStartPositionDiscrepancy { get; set; } = 0.5f;

        [DataMember(Name = "incomplete_path_retry_interval", IsRequired = true)]
        public float IncompletePathRetryInterval { get; set; } = 5;

        public BotPathingConfig()
        {

        }
    }
}
