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
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "min_time_before_jumping")]
        public float MinTimeBeforeJumping { get; set; } = 6;

        [DataMember(Name = "jump_debounce_time")]
        public float JumpDebounceTime { get; set; } = 4;

        [DataMember(Name = "min_time_before_vaulting")]
        public float MinTimeBeforeVaulting { get; set; } = 8;

        [DataMember(Name = "vault_debounce_time")]
        public float VaultDebounceTime { get; set; } = 4;

        public StuckBotRemediesConfig()
        {

        }
    }
}
