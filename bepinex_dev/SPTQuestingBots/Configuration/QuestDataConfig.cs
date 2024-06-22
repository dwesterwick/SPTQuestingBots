using System.Collections.Generic;
using Newtonsoft.Json;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.Configuration
{
    public class QuestDataConfig
    {
        [JsonProperty("templates")]
        public RawQuestClass[] Templates { get; set; } = new RawQuestClass[0];

        [JsonProperty("quests")]
        public Models.Quest[] Quests { get; set; } = new Models.Quest[0];

        [JsonProperty("settings")]
        public Dictionary<string, Dictionary<string, object>> Settings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        [JsonProperty("zoneAndItemPositions")]
        public Dictionary<string, SerializableVector3> ZoneAndItemPositions { get; set; } = new Dictionary<string, SerializableVector3>();

        public QuestDataConfig()
        {

        }
    }
}
