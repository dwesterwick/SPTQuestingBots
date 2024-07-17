using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotQuestingRequirementsConfig
    {
        [JsonProperty("exclude_bots_by_level")]
        public bool ExcludeBotsByLevel { get; set; } = false;

        [JsonProperty("repeat_quest_delay")]
        public float RepeatQuestDelay { get; set; } = 300;

        [JsonProperty("max_time_per_quest")]
        public float MaxTimePerQuest { get; set; } = 300;

        [JsonProperty("min_hydration")]
        public float MinHydration { get; set; } = 50;

        [JsonProperty("min_energy")]
        public float MinEnergy { get; set; } = 50;

        [JsonProperty("min_health_head")]
        public float MinHealthHead { get; set; } = 50;

        [JsonProperty("min_health_chest")]
        public float MinHealthChest { get; set; } = 50;

        [JsonProperty("min_health_stomach")]
        public float MinHealthStomach { get; set; } = 50;

        [JsonProperty("min_health_legs")]
        public float MinHealthLegs { get; set; } = 50;

        [JsonProperty("max_overweight_percentage")]
        public float MaxOverweightPercentage { get; set; } = 100;

        [JsonProperty("search_time_after_combat")]
        public SearchTimeAfterCombatConfig SearchTimeAfterCombat { get; set; } = new SearchTimeAfterCombatConfig();

        [JsonProperty("hearing_sensor")]
        public HearingSensorConfig HearingSensor { get; set; } = new HearingSensorConfig();

        [JsonProperty("break_for_looting")]
        public BreakForLootingConfig BreakForLooting { get; set; } = new BreakForLootingConfig();

        [JsonProperty("max_follower_distance")]
        public MaxFollowerDistanceConfig MaxFollowerDistance { get; set; } = new MaxFollowerDistanceConfig();

        public BotQuestingRequirementsConfig()
        {

        }
    }
}
