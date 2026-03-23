using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestingConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "bot_pathing_update_interval_ms", IsRequired = true)]
        public float BotPathingUpdateInterval { get; set; } = 100;

        [DataMember (Name = "brain_layer_priorities", IsRequired = true)]
        public BrainLayerPrioritiesOptionsConfig BrainLayerPriorities { get; set; } = new BrainLayerPrioritiesOptionsConfig();

        [DataMember(Name = "quest_selection_timeout", IsRequired = true)]
        public float QuestSelectionTimeout { get; set; } = 2000;

        [DataMember(Name = "btr_run_distance", IsRequired = true)]
        public float BTRRunDistance { get; set; } = 40;

        [DataMember(Name = "allowed_bot_types_for_questing", IsRequired = true)]
        public BotTypeConfig AllowedBotTypesForQuesting { get; set; } = new BotTypeConfig();

        [DataMember(Name = "stuck_bot_detection", IsRequired = true)]
        public StuckBotDetectionConfig StuckBotDetection { get; set; } = new StuckBotDetectionConfig();

        [DataMember(Name = "unlocking_doors", IsRequired = true)]
        public UnlockingDoorsConfig UnlockingDoors { get; set; } = new UnlockingDoorsConfig();

        [DataMember(Name = "min_time_between_switching_objectives", IsRequired = true)]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [DataMember(Name = "default_wait_time_after_objective_completion", IsRequired = true)]
        public float DefaultWaitTimeAfterObjectiveCompletion { get; set; } = 10;

        [DataMember(Name = "update_bot_zone_after_stopping", IsRequired = true)]
        public bool UpdateBotZoneAfterStopping { get; set; } = true;

        [DataMember(Name = "wait_time_before_planting", IsRequired = true)]
        public float WaitTimeBeforePlanting { get; set; } = 1;

        [DataMember(Name = "quest_generation", IsRequired = true)]
        public QuestGenerationConfig QuestGeneration { get; set; } = new QuestGenerationConfig();

        [DataMember(Name = "bot_search_distances", IsRequired = true)]
        public BotSearchDistanceConfig BotSearchDistances { get; set; } = new BotSearchDistanceConfig();

        [DataMember(Name = "bot_pathing", IsRequired = true)]
        public BotPathingConfig BotPathing { get; set; } = new BotPathingConfig();

        [DataMember(Name = "bot_questing_requirements", IsRequired = true)]
        public BotQuestingRequirementsConfig BotQuestingRequirements { get; set; } = new BotQuestingRequirementsConfig();

        [DataMember(Name = "extraction_requirements", IsRequired = true)]
        public ExtractionRequirementsConfig ExtractionRequirements { get; set; } = new ExtractionRequirementsConfig();

        [DataMember(Name = "sprinting_limitations", IsRequired = true)]
        public SprintingLimitationsConfig SprintingLimitations { get; set; } = new SprintingLimitationsConfig();

        [DataMember(Name = "bot_quests", IsRequired = true)]
        public BotQuestsConfig BotQuests { get; set; } = new BotQuestsConfig();

        public QuestingConfig()
        {

        }
    }
}
