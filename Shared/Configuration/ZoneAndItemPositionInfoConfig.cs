using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ZoneAndItemPositionInfoConfig
    {
        [DataMember(Name = "position")]
        public SerializableVector3 Position { get; private set; } = null!;

        [DataMember(Name = "mustUnlockNearbyDoor")]
        public bool MustUnlockNearbyDoor { get; private set; } = false;

        [DataMember(Name = "nearbyDoorSearchRadius")]
        public float NearbyDoorSearchRadius { get; private set; } = 5;

        [DataMember(Name = "nearbyDoorInteractionPosition")]
        public SerializableVector3 NearbyDoorInteractionPosition { get; private set; } = null!;

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
