using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class QuestTemplatesConfig
    {
        [JsonProperty("quests")]
        public RawQuestClass[] Quests { get; set; } = new RawQuestClass[0];

        public QuestTemplatesConfig()
        {

        }
    }
}
