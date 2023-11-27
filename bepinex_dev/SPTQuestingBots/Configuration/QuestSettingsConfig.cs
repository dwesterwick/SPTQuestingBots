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
        [JsonProperty("priority")]
        public int Priority { get; set; } = 10;

        [JsonProperty("chance")]
        public float Chance { get; set; } = 50;

        [JsonProperty("max_bots_per_quest")]
        public int MaxBotsPerQuest { get; set; } = 10;

        [JsonProperty("min_distance")]
        public float MinDistance { get; set; } = 10;

        [JsonProperty("max_distance")]
        public float MaxDistance { get; set; } = 9999;

        [JsonProperty("max_raid_ET")]
        public float MaxRaidET { get; set; } = 999;

        [JsonProperty("chanceOfHavingKeys")]
        public float ChanceOfHavingKeys { get; set; } = 25;

        [JsonProperty("level_range")]
        public double[][] LevelRange { get; set; } = new double[0][];

        public QuestSettingsConfig()
        {

        }
    }
}
