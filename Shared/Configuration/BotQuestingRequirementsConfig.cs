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
        [DataMember(Name = "exclude_bots_by_level", EmitDefaultValue = false, IsRequired = true)]
        public bool ExcludeBotsByLevel { get; set; } = false;

        [DataMember(Name = "repeat_quest_delay", EmitDefaultValue = false, IsRequired = true)]
        public float RepeatQuestDelay { get; set; } = 300;

        [DataMember(Name = "max_time_per_quest", EmitDefaultValue = false, IsRequired = true)]
        public float MaxTimePerQuest { get; set; } = 300;

        [DataMember(Name = "min_hydration", EmitDefaultValue = false, IsRequired = true)]
        public float MinHydration { get; set; } = 50;

        [DataMember(Name = "min_energy", EmitDefaultValue = false, IsRequired = true)]
        public float MinEnergy { get; set; } = 50;

        [DataMember(Name = "min_health_head", EmitDefaultValue = false, IsRequired = true)]
        public float MinHealthHead { get; set; } = 50;

        [DataMember(Name = "min_health_chest", EmitDefaultValue = false, IsRequired = true)]
        public float MinHealthChest { get; set; } = 50;

        [DataMember(Name = "min_health_stomach", EmitDefaultValue = false, IsRequired = true)]
        public float MinHealthStomach { get; set; } = 50;

        [DataMember(Name = "min_health_legs", EmitDefaultValue = false, IsRequired = true)]
        public float MinHealthLegs { get; set; } = 50;

        [DataMember(Name = "max_overweight_percentage", EmitDefaultValue = false, IsRequired = true)]
        public float MaxOverweightPercentage { get; set; } = 100;

        [DataMember(Name = "search_time_after_combat", EmitDefaultValue = false, IsRequired = true)]
        public SearchTimeAfterCombatConfig SearchTimeAfterCombat { get; set; } = new SearchTimeAfterCombatConfig();

        [DataMember(Name = "hearing_sensor", EmitDefaultValue = false, IsRequired = true)]
        public HearingSensorConfig HearingSensor { get; set; } = new HearingSensorConfig();

        [DataMember(Name = "break_for_looting", EmitDefaultValue = false, IsRequired = true)]
        public BreakForLootingConfig BreakForLooting { get; set; } = new BreakForLootingConfig();

        [DataMember(Name = "max_follower_distance", EmitDefaultValue = false, IsRequired = true)]
        public MaxFollowerDistanceConfig MaxFollowerDistance { get; set; } = new MaxFollowerDistanceConfig();

        public BotQuestingRequirementsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
