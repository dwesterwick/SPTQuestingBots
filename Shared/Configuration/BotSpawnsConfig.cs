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
        [DataMember(Name = "enabled", EmitDefaultValue = false, IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "blacklisted_pmc_bot_brains", EmitDefaultValue = false, IsRequired = true)]
        public string[] BlacklistedPMCBotBrains { get; set; } = Array.Empty<string>();

        [DataMember(Name = "spawn_retry_time", EmitDefaultValue = false, IsRequired = true)]
        public float SpawnRetryTime { get; set; } = 10;

        [DataMember(Name = "delay_game_start_until_bot_gen_finishes", EmitDefaultValue = false, IsRequired = true)]
        public bool DelayGameStartUntilBotGenFinishes { get; set; } = false;

        [DataMember(Name = "spawn_initial_bosses_first", EmitDefaultValue = false, IsRequired = true)]
        public bool SpawnInitialBossesFirst { get; set; } = true;

        [DataMember(Name = "eft_new_spawn_system_adjustments", EmitDefaultValue = false, IsRequired = true)]
        public EftNewSpawnSystemAdjustmentsConfig EftNewSpawnSystemAdjustments { get; set; } = new EftNewSpawnSystemAdjustmentsConfig();

        [DataMember(Name = "bot_cap_adjustments", EmitDefaultValue = false, IsRequired = true)]
        public BotCapAdjustmentsConfig BotCapAdjustments { get; set; } = new BotCapAdjustmentsConfig();

        [DataMember(Name = "limit_initial_boss_spawns", EmitDefaultValue = false, IsRequired = true)]
        public LimitInitialBossSpawnsConfig LimitInitialBossSpawns { get; set; } = new LimitInitialBossSpawnsConfig();

        [DataMember(Name = "max_alive_bots", EmitDefaultValue = false, IsRequired = true)]
        public Dictionary<string, int> MaxAliveBots { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "pmc_hostility_adjustments", EmitDefaultValue = false, IsRequired = true)]
        public PMCHostilityAdjustmentsConfig PMCHostilityAdjustments { get; set; } = new PMCHostilityAdjustmentsConfig();

        [DataMember(Name = "pmcs", EmitDefaultValue = false, IsRequired = true)]
        public BotSpawnTypeConfig PMCs { get; set; } = new BotSpawnTypeConfig();

        [DataMember(Name = "player_scavs", EmitDefaultValue = false, IsRequired = true)]
        public BotSpawnTypeConfig PScavs { get; set; } = new BotSpawnTypeConfig();

        public BotSpawnsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
