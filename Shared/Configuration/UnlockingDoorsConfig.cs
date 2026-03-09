using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class UnlockingDoorsConfig
    {
        [DataMember(Name = "enabled")]
        public BotTypeConfig Enabled { get; set; } = new BotTypeConfig();

        [DataMember(Name = "search_radius")]
        public float SearchRadius { get; set; } = 25;

        [DataMember(Name = "max_distance_to_unlock")]
        public float MaxDistanceToUnlock { get; set; } = 0.5f;

        [DataMember(Name = "door_approach_position_search_radius")]
        public float DoorApproachPositionSearchRadius { get; set; } = 0.75f;

        [DataMember(Name = "door_approach_position_search_offset")]
        public float DoorApproachPositionSearchOffset { get; set; } = -0.5f;

        [DataMember(Name = "pause_time_after_unlocking")]
        public float PauseTimeAfterUnlocking { get; set; } = 5;

        [DataMember(Name = "debounce_time")]
        public float DebounceTime { get; set; } = 1;

        [DataMember(Name = "default_chance_of_bots_having_keys")]
        public float DefaultChanceOfBotsHavingKeys { get; set; } = 25;

        public UnlockingDoorsConfig()
        {

        }
    }
}
