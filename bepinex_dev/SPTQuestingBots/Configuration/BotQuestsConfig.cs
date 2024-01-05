using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotQuestsConfig
    {
        [JsonProperty("distance_randomness")]
        public int DistanceRandomness { get; set; } = 50;

        [JsonProperty("desirability_randomness")]
        public int DesirabilityRandomness { get; set; } = 50;

        [JsonProperty("distance_weighting")]
        public float DistanceWeighting { get; set; } = 1;

        [JsonProperty("blacklisted_boss_hunter_bosses")]
        public string[] BlacklistedBossHunterBosses { get; set; } = new string[0];

        [JsonProperty("airdrop_bot_interest_time")]
        public float AirdropBotInterestTime { get; set; } = 1800;

        [JsonProperty("eft_quests")]
        public QuestSettingsConfig EFTQuests { get; set; } = new QuestSettingsConfig();

        [JsonProperty("spawn_rush")]
        public QuestSettingsConfig SpawnRush { get; set; } = new QuestSettingsConfig();

        [JsonProperty("spawn_point_wander")]
        public QuestSettingsConfig SpawnPointWander { get; set; } = new QuestSettingsConfig();

        [JsonProperty("boss_hunter")]
        public QuestSettingsConfig BossHunter { get; set; } = new QuestSettingsConfig();

        [JsonProperty("airdrop_chaser")]
        public QuestSettingsConfig AirdropChaser { get; set; } = new QuestSettingsConfig();

        public BotQuestsConfig()
        {

        }
    }
}
