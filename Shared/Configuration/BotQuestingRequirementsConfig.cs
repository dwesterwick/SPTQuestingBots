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
        [DataMember(Name = "exclude_bots_by_level", IsRequired = true)]
        public bool ExcludeBotsByLevel { get; set; } = true;

        [DataMember(Name = "repeat_quest_delay", IsRequired = true)]
        public float RepeatQuestDelay { get; set; } = 300;

        [DataMember(Name = "max_time_per_quest", IsRequired = true)]
        public float MaxTimePerQuest { get; set; } = 300;

        [DataMember(Name = "min_hydration", IsRequired = true)]
        public float MinHydration { get; set; } = 50;

        [DataMember(Name = "min_energy", IsRequired = true)]
        public float MinEnergy { get; set; } = 50;

        [DataMember(Name = "min_health_head", IsRequired = true)]
        public float MinHealthHead { get; set; } = 50;

        [DataMember (Name = "min_health_chest", IsRequired = true)]
        public float MinHealthChest { get; set; } = 50;

        [DataMember(Name = "min_health_stomach", IsRequired = true)]
        public float MinHealthStomach { get; set; } = 50;

        [DataMember(Name = "min_health_legs", IsRequired = true)]
        public float MinHealthLegs { get; set; } = 50;

        [DataMember(Name = "max_overweight_percentage", IsRequired = true)]
        public float MaxOverweightPercentage { get; set; } = 100;

        [DataMember(Name = "search_time_after_combat", IsRequired = true)]
        public SearchTimeAfterCombatConfig SearchTimeAfterCombat { get; set; } = new SearchTimeAfterCombatConfig();

        [DataMember(Name = "hearing_sensor", IsRequired = true)]
        public HearingSensorConfig HearingSensor { get; set; } = new HearingSensorConfig();

        [DataMember(Name = "break_for_looting", IsRequired = true)]
        public BreakForLootingConfig BreakForLooting { get; set; } = new BreakForLootingConfig();

        [DataMember(Name = "max_follower_distance", IsRequired = true)]
        public MaxFollowerDistanceConfig MaxFollowerDistance { get; set; } = new MaxFollowerDistanceConfig();

        public BotQuestingRequirementsConfig()
        {

        }
    }
}
