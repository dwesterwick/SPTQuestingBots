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
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "bot_pathing_update_interval_ms")]
        public float BotPathingUpdateInterval { get; set; } = 100;

        [DataMember(Name = "brain_layer_priorities")]
        public BrainLayerPrioritiesOptionsConfig BrainLayerPriorities { get; set; } = new BrainLayerPrioritiesOptionsConfig();

        [DataMember(Name = "quest_selection_timeout")]
        public float QuestSelectionTimeout { get; set; } = 2000;

        [DataMember(Name = "btr_run_distance")]
        public float BTRRunDistance { get; set; } = 40;

        [DataMember(Name = "allowed_bot_types_for_questing")]
        public BotTypeConfig AllowedBotTypesForQuesting { get; set; } = new BotTypeConfig();

        [DataMember(Name = "stuck_bot_detection")]
        public StuckBotDetectionConfig StuckBotDetection { get; set; } = new StuckBotDetectionConfig();

        [DataMember(Name = "unlocking_doors")]
        public UnlockingDoorsConfig UnlockingDoors { get; set; } = new UnlockingDoorsConfig();

        [DataMember(Name = "min_time_between_switching_objectives")]
        public float MinTimeBetweenSwitchingObjectives { get; set; } = 5;

        [DataMember(Name = "default_wait_time_after_objective_completion")]
        public float DefaultWaitTimeAfterObjectiveCompletion { get; set; } = 10;

        [DataMember(Name = "update_bot_zone_after_stopping")]
        public bool UpdateBotZoneAfterStopping { get; set; } = true;

        [DataMember(Name = "wait_time_before_planting")]
        public float WaitTimeBeforePlanting { get; set; } = 1;

        [DataMember(Name = "quest_generation")]
        public QuestGenerationConfig QuestGeneration { get; set; } = new QuestGenerationConfig();

        [DataMember(Name = "bot_search_distances")]
        public BotSearchDistanceConfig BotSearchDistances { get; set; } = new BotSearchDistanceConfig();

        [DataMember(Name = "bot_pathing")]
        public BotPathingConfig BotPathing { get; set; } = new BotPathingConfig();

        [DataMember(Name = "bot_questing_requirements")]
        public BotQuestingRequirementsConfig BotQuestingRequirements { get; set; } = new BotQuestingRequirementsConfig();

        [DataMember(Name = "extraction_requirements")]
        public ExtractionRequirementsConfig ExtractionRequirements { get; set; } = new ExtractionRequirementsConfig();

        [DataMember(Name = "sprinting_limitations")]
        public SprintingLimitationsConfig SprintingLimitations { get; set; } = new SprintingLimitationsConfig();

        [DataMember(Name = "bot_quests")]
        public BotQuestsConfig BotQuests { get; set; } = new BotQuestsConfig();

        public QuestingConfig()
        {

        }
    }
}
