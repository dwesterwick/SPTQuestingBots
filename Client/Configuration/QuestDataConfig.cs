using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestDataConfig
    {
        [DataMember(Name = "templates")]
        public RawQuestClass[] Templates { get; set; } = Array.Empty<RawQuestClass>();

        [DataMember(Name = "quests")]
        public Models.Questing.Quest[] Quests { get; set; } = Array.Empty<Models.Questing.Quest>();

        [DataMember(Name = "settings")]
        public Dictionary<string, Dictionary<string, object>> Settings { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        [DataMember(Name = "zoneAndItemPositions")]
        public Dictionary<string, ZoneAndItemPositionInfoConfig> ZoneAndItemPositions { get; set; } = new Dictionary<string, ZoneAndItemPositionInfoConfig>();

        public QuestDataConfig()
        {

        }
    }
}
