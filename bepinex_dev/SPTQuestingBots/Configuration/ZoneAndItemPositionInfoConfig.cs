using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.Configuration
{
    public class ZoneAndItemPositionInfoConfig
    {
        [JsonProperty("position")]
        public SerializableVector3 Position { get; private set; } = null;

        [JsonProperty("mustUnlockNearbyDoor")]
        public bool MustUnlockNearbyDoor { get; private set; } = false;

        [JsonProperty("nearbyDoorSearchRadius")]
        public float NearbyDoorSearchRadius { get; private set; } = 5;

        [JsonProperty("nearbyDoorInteractionPosition")]
        public SerializableVector3 NearbyDoorInteractionPosition { get; private set; } = null;

        public ZoneAndItemPositionInfoConfig()
        {

        }

        public ZoneAndItemPositionInfoConfig(SerializableVector3 _position, bool _mustUnlockNearbyDoor, float _nearbyDoorSearchRadius, SerializableVector3 _nearbyDoorInteractionPosition)
        {
            Position = _position;
            MustUnlockNearbyDoor = _mustUnlockNearbyDoor;
            NearbyDoorSearchRadius = _nearbyDoorSearchRadius;
            NearbyDoorInteractionPosition = _nearbyDoorInteractionPosition;
        }
    }
}
