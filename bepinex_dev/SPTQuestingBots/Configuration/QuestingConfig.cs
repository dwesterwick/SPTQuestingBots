using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestingConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("bot_pathing_update_interval_ms")]
        public float BotPathingUpdateInterval { get; set; } = 100;

        [JsonProperty("brain_layer_priority")]
        public int BrainLayerPriority { get; set; } = 21;

        [JsonProperty("quest_selection_timeout")]
        public float QuestSelectionTimeout { get; set; } = 2000;

        [JsonProperty("allowed_bot_types_for_questing")]
        public BotTypeConfig AllowedBotTypesForQuesting { get; set; } = new BotTypeConfig();

        [JsonProperty("search_time_after_combat")]
        public MinMaxConfig SearchTimeAfterCombat { get; set; } = new MinMaxConfig();

        [JsonProperty("stuck_bot_detection")]
        public StuckBotDetectionConfig StuckBotDetection { get; set; } = new StuckBotDetectionConfig();

        [JsonProperty("unlocking_doors")]
        public UnlockingDoorsConfig UnlockingDoors { get; set; } = new UnlockingDoorsConfig();

        [JsonProperty("min_time_between_switching_objectives")]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [JsonProperty("wait_time_before_planting")]
        public float WaitTimeBeforePlanting { get; set; } = 1;

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
