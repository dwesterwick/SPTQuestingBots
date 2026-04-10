using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class StuckBotRemediesConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "min_time_before_jumping", IsRequired = true)]
        public float MinTimeBeforeJumping { get; set; } = 6;

        [DataMember(Name = "jump_debounce_time", IsRequired = true)]
        public float JumpDebounceTime { get; set; } = 4;

        [DataMember(Name = "min_time_before_vaulting", IsRequired = true)]
        public float MinTimeBeforeVaulting { get; set; } = 8;

        [DataMember(Name = "vault_debounce_time", IsRequired = true)]
        public float VaultDebounceTime { get; set; } = 4;

        public StuckBotRemediesConfig()
        {

        }
    }
}
