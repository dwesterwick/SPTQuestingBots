using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class UnlockingDoorsConfig
    {
        [JsonProperty("enabled")]
        public BotTypeConfig Enabled { get; set; } = new BotTypeConfig();

        [JsonProperty("search_radius")]
        public float SearchRadius { get; set; } = 25;

        [JsonProperty("max_distance_to_unlock")]
        public float MaxDistanceToUnlock { get; set; } = 0.5f;

        [JsonProperty("door_approach_position_search_radius")]
        public float DoorApproachPositionSearchRadius { get; set; } = 0.75f;

        [JsonProperty("door_approach_position_search_offset")]
        public float DoorApproachPositionSearchOffset { get; set; } = -0.5f;

        [JsonProperty("pause_time_after_unlocking")]
        public float PauseTimeAfterUnlocking { get; set; } = 5;

        [JsonProperty("debounce_time")]
        public float DebounceTime { get; set; } = 1;

        [JsonProperty("default_chance_of_bots_having_keys")]
        public float DefaultChanceOfBotsHavingKeys { get; set; } = 25;

        public UnlockingDoorsConfig()
        {

        }
    }
}
