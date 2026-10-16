using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots
{
    public struct BotGeneratorProgressPacket : INetSerializable
    {
        public int RemainingBotGenerators { get; set; }
        public string CurrentBotGeneratorType { get; set; }
        public int CurrentBotGeneratorProgress { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            RemainingBotGenerators = reader.GetInt();
            CurrentBotGeneratorType = reader.GetString();
            CurrentBotGeneratorProgress = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(RemainingBotGenerators);
            writer.Put(CurrentBotGeneratorType);
            writer.Put(CurrentBotGeneratorProgress);
        }
    }

    public struct BotDebugStaticInfoPacket : INetSerializable, IClientBotDebugStaticInfo
    {
        public int NetId { get; set; }
        public int BotId { get; set; }
        public bool IsInitialized { get; set; }
        public EBotType BotType { get; set; }
        public string CorrectedNickname { get; set; }
        public int Level { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            BotId = reader.GetInt();
            IsInitialized = reader.GetBool();
            BotType = reader.GetEnum<EBotType>();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(BotId);
            writer.Put(IsInitialized);
            writer.PutEnum(BotType);
        }

        public static BotDebugStaticInfoPacket Create(FikaBot bot)
        {
            return new BotDebugStaticInfoPacket()
            {
                NetId = bot.NetId,
                BotId = bot.Id,
                IsInitialized = bot.AIData.BotOwner.IsInitialized(),
                BotType = Controllers.BotRegistrationManager.GetBotType(bot.AIData.BotOwner),
            };
        }
    }

    public struct BotDebugCurrentInfoPacket : INetSerializable, IClientBotDebugCurrentInfo
    {
        public int NetId { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
        }

        public static BotDebugCurrentInfoPacket Create(FikaBot bot)
        {
            return new BotDebugCurrentInfoPacket()
            {
                NetId = bot.NetId,
            };
        }
    }
}
