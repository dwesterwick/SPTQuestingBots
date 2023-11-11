using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class QuestingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("max_calc_time_per_frame_ms")]
        public float MaxCalcTimePerFrame { get; set; } = 5;

        [JsonProperty("bot_pathing_update_interval_ms")]
        public float BotPathingUpdateInterval { get; set; } = 100;

        [JsonProperty("brain_layer_priority")]
        public int BrainLayerPriority { get; set; } = 21;

        [JsonProperty("allowed_bot_types_for_questing")]
        public AllowedBotTypesForQuestingConfig AllowedBotTypesForQuesting { get; set; } = new AllowedBotTypesForQuestingConfig();

        [JsonProperty("search_time_after_combat")]
        public MinMaxConfig SearchTimeAfterCombat { get; set; } = new MinMaxConfig();

        [JsonProperty("stuck_bot_detection")]
        public StuckBotDetectionConfig StuckBotDetection { get; set; } = new StuckBotDetectionConfig();

        [JsonProperty("min_time_between_switching_objectives")]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [JsonProperty("quest_generation")]
        public QuestGenerationConfig QuestGeneration { get; set; } = new QuestGenerationConfig();

        [JsonProperty("bot_search_distances")]
        public BotSearchDistanceConfig BotSearchDistances { get; set; } = new BotSearchDistanceConfig();

        [JsonProperty("bot_questing_requirements")]
        public BotQuestingRequirementsConfig BotQuestingRequirements { get; set; } = new BotQuestingRequirementsConfig();

        [JsonProperty("bot_quests")]
        public BotQuestsConfig BotQuests { get; set; } = new BotQuestsConfig();

        public QuestingConfig()
        {

        }
    }
}
