using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.Questing
{
    public class SptRawQuestClass : RawQuestClass
    {
        [JsonProperty("QuestName")]
        public string SptQuestName = string.Empty;

        public SptRawQuestClass() : base()
        {

        }

        public SptRawQuestClass(string id) : base(id)
        {

        }
    }
}
