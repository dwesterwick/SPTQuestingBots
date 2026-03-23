using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotQuestsConfig
    {
        [DataMember(Name = "distance_randomness", EmitDefaultValue = false, IsRequired = true)]
        public int DistanceRandomness { get; set; } = 50;

        [DataMember(Name = "desirability_randomness", EmitDefaultValue = false, IsRequired = true)]
        public int DesirabilityRandomness { get; set; } = 50;

        [DataMember(Name = "distance_weighting", EmitDefaultValue = false, IsRequired = true)]
        public float DistanceWeighting { get; set; } = 1;

        [DataMember(Name = "desirability_weighting", EmitDefaultValue = false, IsRequired = true)]
        public float DesirabilityWeighting { get; set; } = 1;

        [DataMember(Name = "desirability_camping_multiplier", EmitDefaultValue = false, IsRequired = true)]
        public float DesirabilityCampingMultiplier { get; set; } = 1;

        [DataMember(Name = "desirability_sniping_multiplier", EmitDefaultValue = false, IsRequired = true)]
        public float DesirabilitySnipingMultiplier { get; set; } = 1;

        [DataMember(Name = "desirability_active_quest_multiplier", EmitDefaultValue = false, IsRequired = true)]
        public float DesirabilityActiveQuestMultiplier { get; set; } = 1;

        [DataMember(Name = "exfil_direction_weighting", EmitDefaultValue = false, IsRequired = true)]
        public Dictionary<string, float> ExfilDirectionWeighting { get; set; } = new Dictionary<string, float>();

        [DataMember(Name = "exfil_direction_max_angle", EmitDefaultValue = false, IsRequired = true)]
        public float ExfilDirectionMaxAngle { get; set; } = 90;

        [DataMember(Name = "exfil_reached_min_fraction", EmitDefaultValue = false, IsRequired = true)]
        public float ExfilReachedMinFraction { get; set; } = 0.2f;

        [DataMember(Name = "blacklisted_boss_hunter_bosses", EmitDefaultValue = false, IsRequired = true)]
        public string[] BlacklistedBossHunterBosses { get; set; } = Array.Empty<string>();

        [DataMember(Name = "airdrop_bot_interest_time", EmitDefaultValue = false, IsRequired = true)]
        public float AirdropBotInterestTime { get; set; } = 1800;

        [DataMember(Name = "elimination_quest_search_time", EmitDefaultValue = false, IsRequired = true)]
        public float EliminationQuestSearchTime { get; set; } = 60;

        [DataMember(Name = "eft_quests", EmitDefaultValue = false, IsRequired = true)]
        public QuestSettingsConfig EFTQuests { get; set; } = new QuestSettingsConfig();

        [DataMember(Name = "lightkeeper_island_quests", EmitDefaultValue = false, IsRequired = true)]
        public LightkeeperIslandQuestsConfig LightkeeperIslandQuests { get; set; } = new LightkeeperIslandQuestsConfig();

        [DataMember(Name = "spawn_rush", EmitDefaultValue = false, IsRequired = true)]
        public QuestSettingsConfig SpawnRush { get; set; } = new QuestSettingsConfig();

        [DataMember(Name = "spawn_point_wander", EmitDefaultValue = false, IsRequired = true)]
        public QuestSettingsConfig SpawnPointWander { get; set; } = new QuestSettingsConfig();

        [DataMember(Name = "boss_hunter", EmitDefaultValue = false, IsRequired = true)]
        public QuestSettingsConfig BossHunter { get; set; } = new QuestSettingsConfig();

        [DataMember(Name = "airdrop_chaser", EmitDefaultValue = false, IsRequired = true)]
        public QuestSettingsConfig AirdropChaser { get; set; } = new QuestSettingsConfig();

        public BotQuestsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
