using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestSettingsConfig
    {
        [DataMember(Name = "desirability")]
        public float Desirability { get; set; } = 50;

        [DataMember(Name = "pmcsOnly")]
        public bool PMCsOnly { get; set; } = false;

        [DataMember(Name = "max_bots_per_quest")]
        public int MaxBotsPerQuest { get; set; } = 10;

        [DataMember(Name = "min_distance")]
        public float MinDistance { get; set; } = 10;

        [DataMember(Name = "max_distance")]
        public float MaxDistance { get; set; } = 9999;

        [DataMember(Name = "max_raid_ET")]
        public float MaxRaidET { get; set; } = 999;

        [DataMember(Name = "chance_of_having_keys")]
        public float ChanceOfHavingKeys { get; set; } = 25;

        [DataMember(Name = "match_looting_behavior_distance")]
        public float MatchLootingBehaviorDistance { get; set; } = 0;

        [DataMember(Name = "min_level")]
        public int MinLevel { get; set; } = 0;

        [DataMember(Name = "max_level")]
        public int MaxLevel { get; set; } = 99;

        [DataMember(Name = "level_range")]
        public double[][] LevelRange { get; set; } = Array.Empty<double[]>();

        public QuestSettingsConfig()
        {

        }

        public static void ApplyQuestSettingsFromConfig(Models.Questing.Quest quest, QuestSettingsConfig settings)
        {
            quest.Desirability = settings.Desirability;
            quest.PMCsOnly = settings.PMCsOnly;
            quest.MaxBots = settings.MaxBotsPerQuest;
            quest.MaxRaidET = settings.MaxRaidET;
            quest.MinLevel = settings.MinLevel;
            quest.MaxLevel = settings.MaxLevel;
        }

        public static void ApplyQuestSettingsFromConfig(Models.Questing.QuestObjective objective, QuestSettingsConfig settings)
        {
            objective.MinDistanceFromBot = settings.MinDistance;
            objective.MaxDistanceFromBot = settings.MaxDistance;
        }
    }
}
