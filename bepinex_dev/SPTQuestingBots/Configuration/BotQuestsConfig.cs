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
        [JsonProperty("eft_quests")]
        public QuestSettingsConfig EFTQuests { get; set; } = new QuestSettingsConfig();

        [JsonProperty("spawn_rush")]
        public QuestSettingsConfig SpawnRush { get; set; } = new QuestSettingsConfig();

        [JsonProperty("spawn_point_wander")]
        public QuestSettingsConfig SpawnPointWander { get; set; } = new QuestSettingsConfig();

        public BotQuestsConfig()
        {

        }
    }
}
