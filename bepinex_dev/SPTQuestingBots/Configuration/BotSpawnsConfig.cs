using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotSpawnsConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("blacklisted_pmc_bot_brains")]
        public string[] BlacklistedPMCBotBrains { get; set; } = new string[0];

        [JsonProperty("spawn_retry_time")]
        public float SpawnRetryTime { get; set; } = 10;

        [JsonProperty("advanced_eft_bot_count_management")]
        public bool AdvancedEFTBotCountManagement { get; set; } = false;

        [JsonProperty("bot_cap_adjustments")]
        public BotCapAdjustmentsConfig BotCapAdjustments { get; set; } = new BotCapAdjustmentsConfig();

        [JsonProperty("limit_initial_boss_spawns")]
        public LimitInitialBossSpawnsConfig LimitInitialBossSpawns { get; set; } = new LimitInitialBossSpawnsConfig();

        [JsonProperty("max_alive_bots")]
        public Dictionary<string, int> MaxAliveBots { get; set; } = new Dictionary<string, int>();

        [JsonProperty("pmcs")]
        public BotSpawnTypeConfig PMCs { get; set; } = new BotSpawnTypeConfig();

        [JsonProperty("player_scavs")]
        public BotSpawnTypeConfig PScavs { get; set; } = new BotSpawnTypeConfig();

        public BotSpawnsConfig()
        {

        }
    }
}
