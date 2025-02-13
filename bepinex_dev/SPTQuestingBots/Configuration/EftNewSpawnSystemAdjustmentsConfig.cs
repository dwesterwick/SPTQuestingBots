using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class EftNewSpawnSystemAdjustmentsConfig
    {
        [JsonProperty("non_wave_retry_delay_after_blocked")]
        public float NonWaveRetryDelayAfterBlocked { get; set; } = 20;

        [JsonProperty("scav_spawn_rate_time_window")]
        public float ScavSpawnRateTimeWindow { get; set; } = 300;

        public EftNewSpawnSystemAdjustmentsConfig()
        {

        }
    }
}
