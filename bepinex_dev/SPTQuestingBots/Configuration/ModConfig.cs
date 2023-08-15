using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPTQuestingBots.Configuration;

namespace QuestingBots.Configuration
{
    public class ModConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("debug")]
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [JsonProperty("max_calc_time_per_frame_ms")]
        public float MaxCalcTimePerFrame { get; set; } = 5;

        [JsonProperty("brain_layer_priority")]
        public int BrainLayerPriority { get; set; } = 21;

        [JsonProperty("search_time_after_combat")]
        public MinMaxConfig SearchTimeAfterCombat { get; set; } = new MinMaxConfig();

        [JsonProperty("min_time_between_switching_objectives")]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [JsonProperty("quest_generation")]
        public QuestGenerationConfig QuestGeneration { get; set; } = new QuestGenerationConfig();

        [JsonProperty("bot_search_distances")]
        public BotSearchDistanceConfig BotSearchDistances { get; set; } = new BotSearchDistanceConfig();

        [JsonProperty("bot_quests")]
        public BotQuestsConfig BotQuests { get; set; } = new BotQuestsConfig();

        [JsonProperty("initial_PMC_spawns")]
        public InitialPMCSpawnsConfig InitialPMCSpawns { get; set; } = new InitialPMCSpawnsConfig();

        public ModConfig()
        {

        }
    }
}
