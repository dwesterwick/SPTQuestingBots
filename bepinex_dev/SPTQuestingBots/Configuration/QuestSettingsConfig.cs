using Newtonsoft.Json;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class QuestSettingsConfig
    {
        [JsonProperty("desirability")]
        public float Desirability { get; set; } = 50;

        [JsonProperty("pmcsOnly")]
        public bool PMCsOnly { get; set; } = false;

        [JsonProperty("max_bots_per_quest")]
        public int MaxBotsPerQuest { get; set; } = 10;

        [JsonProperty("min_distance")]
        public float MinDistance { get; set; } = 10;

        [JsonProperty("max_distance")]
        public float MaxDistance { get; set; } = 9999;

        [JsonProperty("max_raid_ET")]
        public float MaxRaidET { get; set; } = 999;

        [JsonProperty("chance_of_having_keys")]
        public float ChanceOfHavingKeys { get; set; } = 25;

        [JsonProperty("match_looting_behavior_distance")]
        public float MatchLootingBehaviorDistance { get; set; } = 0;

        [JsonProperty("min_level")]
        public int MinLevel { get; set; } = 0;

        [JsonProperty("max_level")]
        public int MaxLevel { get; set; } = 99;

        [JsonProperty("level_range")]
        public double[][] LevelRange { get; set; } = new double[0][];

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
