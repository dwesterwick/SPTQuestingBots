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

        [JsonProperty("min_other_bots_allowed_to_spawn")]
        public int MinOtherBotsAllowedToSpawn { get; set; } = 4;

        [JsonProperty("max_initial_bosses")]
        public int MaxInitialBosses { get; set; } = 10;

        [JsonProperty("max_initial_rogues")]
        public int MaxInitialRogues { get; set; } = 6;

        [JsonProperty("add_max_players_to_bot_cap")]
        public bool AddMaxPlayersToBotCap { get; set; } = false;

        [JsonProperty("max_additional_bots")]
        public int MaxAdditionalBots { get; set; } = 10;

        [JsonProperty("max_total_bots")]
        public int MaxTotalBots { get; set; } = 40;

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
