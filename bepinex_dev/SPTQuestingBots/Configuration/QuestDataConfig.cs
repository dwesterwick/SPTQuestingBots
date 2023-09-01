using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestDataConfig
    {
        [JsonProperty("templates")]
        public RawQuestClass[] Templates { get; set; } = new RawQuestClass[0];

        [JsonProperty("quests")]
        public Models.Quest[] Quests { get; set; } = new Models.Quest[0];

        public QuestDataConfig()
        {

        }
    }
}
