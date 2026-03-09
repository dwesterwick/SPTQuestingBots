using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotQuestingRequirementsConfig
    {
        [DataMember(Name = "exclude_bots_by_level")]
        public bool ExcludeBotsByLevel { get; set; } = false;

        [DataMember(Name = "repeat_quest_delay")]
        public float RepeatQuestDelay { get; set; } = 300;

        [DataMember(Name = "max_time_per_quest")]
        public float MaxTimePerQuest { get; set; } = 300;

        [DataMember(Name = "min_hydration")]
        public float MinHydration { get; set; } = 50;

        [DataMember(Name = "min_energy")]
        public float MinEnergy { get; set; } = 50;

        [DataMember(Name = "min_health_head")]
        public float MinHealthHead { get; set; } = 50;

        [DataMember(Name = "min_health_chest")]
        public float MinHealthChest { get; set; } = 50;

        [DataMember(Name = "min_health_stomach")]
        public float MinHealthStomach { get; set; } = 50;

        [DataMember(Name = "min_health_legs")]
        public float MinHealthLegs { get; set; } = 50;

        [DataMember(Name = "max_overweight_percentage")]
        public float MaxOverweightPercentage { get; set; } = 100;

        [DataMember(Name = "search_time_after_combat")]
        public SearchTimeAfterCombatConfig SearchTimeAfterCombat { get; set; } = new SearchTimeAfterCombatConfig();

        [DataMember(Name = "hearing_sensor")]
        public HearingSensorConfig HearingSensor { get; set; } = new HearingSensorConfig();

        [DataMember(Name = "break_for_looting")]
        public BreakForLootingConfig BreakForLooting { get; set; } = new BreakForLootingConfig();

        [DataMember(Name = "max_follower_distance")]
        public MaxFollowerDistanceConfig MaxFollowerDistance { get; set; } = new MaxFollowerDistanceConfig();

        public BotQuestingRequirementsConfig()
        {

        }
    }
}
