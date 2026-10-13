using EFT;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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
        public int BotId { get; set; }
        public bool IsInitialized { get; set; }
        public EBotType BotType { get; set; }
        public string CorrectedNickname { get; set; }
        public int Level { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            BotId = reader.GetInt();
            IsInitialized = reader.GetBool();
            BotType = reader.GetEnum<EBotType>();
            CorrectedNickname = reader.GetString();
            Level = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(BotId);
            writer.Put(IsInitialized);
            writer.PutEnum(BotType);
            writer.Put(CorrectedNickname);
            writer.Put(Level);
        }

        public static BotDebugStaticInfoPacket Create(BotOwner bot)
        {
            return new BotDebugStaticInfoPacket()
            {
                BotId = bot.Id,
                IsInitialized = bot.IsInitialized(),
                BotType = Controllers.BotRegistrationManager.GetBotType(bot),
                CorrectedNickname = bot.GetCorrectedNickname(),
                Level = bot.Profile.Info.Level
            };
        }
    }

    public struct BotDebugCurrentInfoPacket : INetSerializable, IClientBotDebugCurrentInfo
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

        public static BotDebugCurrentInfoPacket Create(BotOwner bot)
        {
            return new BotDebugCurrentInfoPacket()
            {
                BotId = bot.Id,
                IsDead = bot.IsDead,
                Position = bot.Position,
            };
        }
    }
}
