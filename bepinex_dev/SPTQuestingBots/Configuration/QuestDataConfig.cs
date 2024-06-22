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
        public Dictionary<string, ZoneAndItemPositionInfo> ZoneAndItemPositions { get; set; } = new Dictionary<string, ZoneAndItemPositionInfo>();

        public QuestDataConfig()
        {

        }
    }

    public class ZoneAndItemPositionInfo
    {
        [JsonProperty("position")]
        public SerializableVector3 Position { get; set; } = null;

        [JsonProperty("mustUnlockNearbyDoor")]
        public bool MustUnlockNearbyDoor = false;

        [JsonProperty("nearbyDoorSearchRadius")]
        public float NearbyDoorSearchRadius = 5;

        [JsonProperty("nearbyDoorInteractionPosition")]
        public SerializableVector3 NearbyDoorInteractionPosition { get; set; } = null;

        public ZoneAndItemPositionInfo()
        {

        }
    }
}
