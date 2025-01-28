using System.Collections.Generic;
using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestDataConfig
    {
        [JsonProperty("templates")]
        public RawQuestClass[] Templates { get; set; } = new RawQuestClass[0];

        [JsonProperty("quests")]
        public Models.Questing.Quest[] Quests { get; set; } = new Models.Questing.Quest[0];

        [JsonProperty("settings")]
        public Dictionary<string, Dictionary<string, object>> Settings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        [JsonProperty("zoneAndItemPositions")]
        public Dictionary<string, ZoneAndItemPositionInfoConfig> ZoneAndItemPositions { get; set; } = new Dictionary<string, ZoneAndItemPositionInfoConfig>();

        public QuestDataConfig()
        {

        }
    }
}
