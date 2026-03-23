using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class EftNewSpawnSystemAdjustmentsConfig
    {
        [DataMember(Name = "non_wave_retry_delay_after_blocked", IsRequired = true)]
        public float NonWaveRetryDelayAfterBlocked { get; set; } = 20;

        [DataMember(Name = "scav_spawn_rate_time_window", IsRequired = true)]
        public float ScavSpawnRateTimeWindow { get; set; } = 300;

        public EftNewSpawnSystemAdjustmentsConfig()
        {

        }
    }
}
