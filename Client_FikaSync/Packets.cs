using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.Models.DebugGizmos;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace QuestingBots
{
    public struct BotDebugInfoPacket : INetSerializable, INetworkBotDebugInfo
    {
        public int BotId { get; set; }
        public bool IsDead { get; set; }
        public ClassVector3 Position { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            BotId = reader.GetInt();
            IsDead = reader.GetBool();
            Position = reader.GetClassVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(BotId);
            writer.Put(IsDead);
            writer.PutClassVector3(Position);
        }
    }
}
