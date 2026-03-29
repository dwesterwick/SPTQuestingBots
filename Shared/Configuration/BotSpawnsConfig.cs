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
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "blacklisted_pmc_bot_brains", IsRequired = true)]
        public string[] BlacklistedPMCBotBrains { get; set; } = Array.Empty<string>();

        [DataMember(Name = "player_scav_brain_conversion_chances_overrides", IsRequired = true)]
        public PlayerScavBrainConversionChancesOverridesConfig PlayerScavBrainConversionChancesOverrides { get; set; } = new PlayerScavBrainConversionChancesOverridesConfig();

        [DataMember(Name = "spawn_retry_time", IsRequired = true)]
        public float SpawnRetryTime { get; set; } = 10;

        [DataMember(Name = "delay_game_start_until_bot_gen_finishes", IsRequired = true)]
        public bool DelayGameStartUntilBotGenFinishes { get; set; } = true;

        [DataMember(Name = "spawn_initial_bosses_first", IsRequired = true)]
        public bool SpawnInitialBossesFirst { get; set; } = true;

        [DataMember(Name = "eft_new_spawn_system_adjustments", IsRequired = true)]
        public EftNewSpawnSystemAdjustmentsConfig EftNewSpawnSystemAdjustments { get; set; } = new EftNewSpawnSystemAdjustmentsConfig();

        [DataMember(Name = "bot_cap_adjustments", IsRequired = true)]
        public BotCapAdjustmentsConfig BotCapAdjustments { get; set; } = new BotCapAdjustmentsConfig();

        [DataMember(Name = "limit_initial_boss_spawns", IsRequired = true)]
        public LimitInitialBossSpawnsConfig LimitInitialBossSpawns { get; set; } = new LimitInitialBossSpawnsConfig();

        [DataMember(Name = "max_alive_bots", IsRequired = true)]
        public Dictionary<string, int> MaxAliveBots { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "pmc_hostility_adjustments", IsRequired = true)]
        public PMCHostilityAdjustmentsConfig PMCHostilityAdjustments { get; set; } = new PMCHostilityAdjustmentsConfig();

        [DataMember(Name = "pmcs", IsRequired = true)]
        public BotSpawnTypeConfig PMCs { get; set; } = new BotSpawnTypeConfig();

        [DataMember(Name = "player_scavs", IsRequired = true)]
        public BotSpawnTypeConfig PScavs { get; set; } = new BotSpawnTypeConfig();

        public BotSpawnsConfig()
        {

        }
    }
}
