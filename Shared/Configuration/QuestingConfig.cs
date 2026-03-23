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
        [DataMember(Name = "enabled", EmitDefaultValue = false, IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "bot_pathing_update_interval_ms", EmitDefaultValue = false, IsRequired = true)]
        public float BotPathingUpdateInterval { get; set; } = 100;

        [DataMember(Name = "brain_layer_priorities", EmitDefaultValue = false, IsRequired = true)]
        public BrainLayerPrioritiesOptionsConfig BrainLayerPriorities { get; set; } = new BrainLayerPrioritiesOptionsConfig();

        [DataMember(Name = "quest_selection_timeout", EmitDefaultValue = false, IsRequired = true)]
        public float QuestSelectionTimeout { get; set; } = 2000;

        [DataMember(Name = "btr_run_distance", EmitDefaultValue = false, IsRequired = true)]
        public float BTRRunDistance { get; set; } = 40;

        [DataMember(Name = "allowed_bot_types_for_questing", EmitDefaultValue = false, IsRequired = true)]
        public BotTypeConfig AllowedBotTypesForQuesting { get; set; } = new BotTypeConfig();

        [DataMember(Name = "stuck_bot_detection", EmitDefaultValue = false, IsRequired = true)]
        public StuckBotDetectionConfig StuckBotDetection { get; set; } = new StuckBotDetectionConfig();

        [DataMember(Name = "unlocking_doors", EmitDefaultValue = false, IsRequired = true)]
        public UnlockingDoorsConfig UnlockingDoors { get; set; } = new UnlockingDoorsConfig();

        [DataMember(Name = "min_time_between_switching_objectives", EmitDefaultValue = false, IsRequired = true)]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [DataMember(Name = "default_wait_time_after_objective_completion", EmitDefaultValue = false, IsRequired = true)]
        public float DefaultWaitTimeAfterObjectiveCompletion { get; set; } = 10;

        [DataMember(Name = "update_bot_zone_after_stopping", EmitDefaultValue = false, IsRequired = true)]
        public bool UpdateBotZoneAfterStopping { get; set; } = true;

        [DataMember(Name = "wait_time_before_planting", EmitDefaultValue = false, IsRequired = true)]
        public float WaitTimeBeforePlanting { get; set; } = 1;

        [DataMember(Name = "quest_generation", EmitDefaultValue = false, IsRequired = true)]
        public QuestGenerationConfig QuestGeneration { get; set; } = new QuestGenerationConfig();

        [DataMember(Name = "bot_search_distances", EmitDefaultValue = false, IsRequired = true)]
        public BotSearchDistanceConfig BotSearchDistances { get; set; } = new BotSearchDistanceConfig();

        [DataMember(Name = "bot_pathing", EmitDefaultValue = false, IsRequired = true)]
        public BotPathingConfig BotPathing { get; set; } = new BotPathingConfig();

        [DataMember(Name = "bot_questing_requirements", EmitDefaultValue = false, IsRequired = true)]
        public BotQuestingRequirementsConfig BotQuestingRequirements { get; set; } = new BotQuestingRequirementsConfig();

        [DataMember(Name = "extraction_requirements", EmitDefaultValue = false, IsRequired = true)]
        public ExtractionRequirementsConfig ExtractionRequirements { get; set; } = new ExtractionRequirementsConfig();

        [DataMember(Name = "sprinting_limitations", EmitDefaultValue = false, IsRequired = true)]
        public SprintingLimitationsConfig SprintingLimitations { get; set; } = new SprintingLimitationsConfig();

        [DataMember(Name = "bot_quests", EmitDefaultValue = false, IsRequired = true)]
        public BotQuestsConfig BotQuests { get; set; } = new BotQuestsConfig();

        public QuestingConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
