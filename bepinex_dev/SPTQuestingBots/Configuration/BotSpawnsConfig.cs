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

        [JsonProperty("delay_game_start_until_bot_gen_finishes")]
        public bool DelayGameStartUntilBotGenFinishes { get; set; } = false;

        [JsonProperty("spawn_initial_bosses_first")]
        public bool SpawnInitialBossesFirst { get; set; } = true;

        [JsonProperty("eft_new_spawn_system_adjustments")]
        public EftNewSpawnSystemAdjustmentsConfig EftNewSpawnSystemAdjustments { get; set; } = new EftNewSpawnSystemAdjustmentsConfig();

        [JsonProperty("bot_cap_adjustments")]
        public BotCapAdjustmentsConfig BotCapAdjustments { get; set; } = new BotCapAdjustmentsConfig();

        [JsonProperty("limit_initial_boss_spawns")]
        public LimitInitialBossSpawnsConfig LimitInitialBossSpawns { get; set; } = new LimitInitialBossSpawnsConfig();

        [JsonProperty("max_alive_bots")]
        public Dictionary<string, int> MaxAliveBots { get; set; } = new Dictionary<string, int>();

        [JsonProperty("pmc_hostility_adjustments")]
        public PMCHostilityAdjustmentsConfig PMCHostilityAdjustments { get; set; } = new PMCHostilityAdjustmentsConfig();

        [JsonProperty("pmcs")]
        public BotSpawnTypeConfig PMCs { get; set; } = new BotSpawnTypeConfig();

        [JsonProperty("player_scavs")]
        public BotSpawnTypeConfig PScavs { get; set; } = new BotSpawnTypeConfig();

        public BotSpawnsConfig()
        {

        }
    }
}
