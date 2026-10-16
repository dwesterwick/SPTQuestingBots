using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.Controllers;
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
        public string BaseBrainName { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            BotId = reader.GetInt();
            IsInitialized = reader.GetBool();
            BotType = reader.GetEnum<EBotType>();
            BaseBrainName = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(BotId);
            writer.Put(IsInitialized);
            writer.PutEnum(BotType);
            writer.Put(BaseBrainName);
        }

        public static BotDebugStaticInfoPacket Create(FikaBot bot)
        {
            return new BotDebugStaticInfoPacket()
            {
                NetId = bot.NetId,
                BotId = bot.Id,
                IsInitialized = bot.AIData.BotOwner.IsInitialized(),
                BotType = Controllers.BotRegistrationManager.GetBotType(bot.AIData.BotOwner),
                BaseBrainName = bot.AIData.BotOwner.Brain.BaseBrain.ShortName()
            };
        }
    }

    public struct BotDebugCurrentInfoPacket : INetSerializable, IClientBotDebugCurrentInfo
    {
        public int NetId { get; set; }
        public EBotState BotState { get; set; }
        public bool IsSleeping { get; set; }
        public int[] EnemyPlayerIds { get; set; }
        public string ActiveLayerName { get; set; }
        public string ActiveLogicName { get; set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            BotState = reader.GetEnum<EBotState>();
            IsSleeping = reader.GetBool();
            EnemyPlayerIds = reader.GetIntArray();
            ActiveLayerName = reader.GetString();
            ActiveLogicName = reader.GetString();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(BotState);
            writer.Put(IsSleeping);
            writer.PutArray(EnemyPlayerIds);
            writer.Put(ActiveLayerName);
            writer.Put(ActiveLogicName);
        }

        public static BotDebugCurrentInfoPacket Create(FikaBot bot)
        {
            return new BotDebugCurrentInfoPacket()
            {
                NetId = bot.NetId,
                BotState = bot.AIData.BotOwner.BotState,
                IsSleeping = BotRegistrationManager.IsBotSleeping(bot.Profile.Id),
                EnemyPlayerIds = bot.AIData.BotOwner.EnemyPlayerIdsArray(),
                ActiveLayerName = bot.AIData.BotOwner.GetActiveLayerName(),
                ActiveLogicName = bot.AIData.BotOwner.GetActiveLogicName(),
            };
        }
    }
}
