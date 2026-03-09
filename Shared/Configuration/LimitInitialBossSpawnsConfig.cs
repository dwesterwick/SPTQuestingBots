using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LimitInitialBossSpawnsConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "disable_rogue_delay")]
        public bool DisableRogueDelay { get; set; } = true;

        [DataMember(Name = "max_initial_bosses")]
        public int MaxInitialBosses { get; set; } = 10;

        [DataMember(Name = "max_initial_rogues")]
        public int MaxInitialRogues { get; set; } = 6;

        public LimitInitialBossSpawnsConfig()
        {

        }
    }
}
