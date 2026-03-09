using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotSpawnsConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "blacklisted_pmc_bot_brains")]
        public string[] BlacklistedPMCBotBrains { get; set; } = Array.Empty<string>();

        [DataMember(Name = "spawn_retry_time")]
        public float SpawnRetryTime { get; set; } = 10;

        [DataMember(Name = "delay_game_start_until_bot_gen_finishes")]
        public bool DelayGameStartUntilBotGenFinishes { get; set; } = false;

        [DataMember(Name = "spawn_initial_bosses_first")]
        public bool SpawnInitialBossesFirst { get; set; } = true;

        [DataMember(Name = "eft_new_spawn_system_adjustments")]
        public EftNewSpawnSystemAdjustmentsConfig EftNewSpawnSystemAdjustments { get; set; } = new EftNewSpawnSystemAdjustmentsConfig();

        [DataMember(Name = "bot_cap_adjustments")]
        public BotCapAdjustmentsConfig BotCapAdjustments { get; set; } = new BotCapAdjustmentsConfig();

        [DataMember(Name = "limit_initial_boss_spawns")]
        public LimitInitialBossSpawnsConfig LimitInitialBossSpawns { get; set; } = new LimitInitialBossSpawnsConfig();

        [DataMember(Name = "max_alive_bots")]
        public Dictionary<string, int> MaxAliveBots { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "pmc_hostility_adjustments")]
        public PMCHostilityAdjustmentsConfig PMCHostilityAdjustments { get; set; } = new PMCHostilityAdjustmentsConfig();

        [DataMember(Name = "pmcs")]
        public BotSpawnTypeConfig PMCs { get; set; } = new BotSpawnTypeConfig();

        [DataMember(Name = "player_scavs")]
        public BotSpawnTypeConfig PScavs { get; set; } = new BotSpawnTypeConfig();

        public BotSpawnsConfig()
        {

        }
    }
}
