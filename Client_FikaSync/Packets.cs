using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using QuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots
{
    public struct BotGeneratorProgressPacket : INetSerializable
    {
        public int RemainingBotGenerators { get; private set; }
        public string CurrentBotGeneratorType { get; private set; }
        public int CurrentBotGeneratorProgress { get; private set; }

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

        public static BotGeneratorProgressPacket Create()
        {
            return new BotGeneratorProgressPacket()
            {
                RemainingBotGenerators = BotGenerator.RemainingBotGenerators,
                CurrentBotGeneratorType = BotGenerator.CurrentBotGeneratorType,
                CurrentBotGeneratorProgress = BotGenerator.CurrentBotGeneratorProgress
            };
        }
    }

    public struct BotDebugStaticInfoPacket : INetSerializable, IBotDebugStaticInfo
    {
        public int NetId { get; private set; }
        public int BotId { get; private set; }
        public bool IsInitialized { get; private set; }
        public EBotType BotType { get; private set; }
        public string BaseBrainName { get; private set; }

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
                BaseBrainName = bot.AIData.BotOwner.Brain?.BaseBrain?.ShortName() ?? "[NOT INITIALIZED]",
            };
        }
    }

    public struct BotDebugCurrentInfoPacket : INetSerializable, IBotDebugCurrentInfo
    {
        public int NetId { get; private set; }
        public EBotState BotState { get; private set; }
        public bool IsSleeping { get; private set; }
        public int[] EnemyPlayerIds { get; private set; }
        public string ActiveLayerName { get; private set; }
        public string ActiveLogicName { get; private set; }
        public bool HasCurrentEnemy { get; private set; }
        public int CurrentEnemyPlayerId { get; private set; }
        public bool HasBoss { get; private set; }
        public int BossPlayerId { get; private set; }
        public bool HasEftPath { get; private set; }
        public ClassVector3 CurrentEftPathCorner { get; private set; }
        public ClassVector3 CurrentEftPathTarget { get; private set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            BotState = reader.GetEnum<EBotState>();
            IsSleeping = reader.GetBool();
            EnemyPlayerIds = reader.GetIntArray();
            ActiveLayerName = reader.GetString();
            ActiveLogicName = reader.GetString();
            HasCurrentEnemy = reader.GetBool();
            CurrentEnemyPlayerId = reader.GetInt();
            HasBoss = reader.GetBool();
            BossPlayerId = reader.GetInt();
            HasEftPath = reader.GetBool();
            CurrentEftPathCorner = reader.GetClassVector3();
            CurrentEftPathTarget = reader.GetClassVector3();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.PutEnum(BotState);
            writer.Put(IsSleeping);
            writer.PutArray(EnemyPlayerIds);
            writer.Put(ActiveLayerName);
            writer.Put(ActiveLogicName);
            writer.Put(HasCurrentEnemy);
            writer.Put(CurrentEnemyPlayerId);
            writer.Put(HasBoss);
            writer.Put(BossPlayerId);
            writer.Put(HasEftPath);
            writer.PutClassVector3(CurrentEftPathCorner);
            writer.PutClassVector3(CurrentEftPathTarget);
        }

        public static BotDebugCurrentInfoPacket Create(FikaBot bot)
        {
            bool hasCurrentEnemy = bot.AIData.BotOwner.HasCurrentEnemy(out int currentEnemyPlayerId);
            bool hasBoss = bot.AIData.BotOwner.HasBoss(out int bossPlayerId);
            bool hasEftPath = bot.AIData.BotOwner.HasEftPath(out Vector3? targetPoint, out Vector3? currentCorner);

            return new BotDebugCurrentInfoPacket()
            {
                NetId = bot.NetId,
                BotState = bot.AIData.BotOwner.BotState,
                IsSleeping = BotRegistrationManager.IsBotSleeping(bot.Profile.Id),
                EnemyPlayerIds = bot.AIData.BotOwner.EnemyPlayerIdsArray(),
                ActiveLayerName = bot.AIData.BotOwner.GetActiveLayerName(),
                ActiveLogicName = bot.AIData.BotOwner.GetActiveLogicName(),
                HasCurrentEnemy = hasCurrentEnemy,
                CurrentEnemyPlayerId = currentEnemyPlayerId,
                HasBoss = hasBoss,
                BossPlayerId = bossPlayerId,
                HasEftPath = hasEftPath,
                CurrentEftPathCorner = currentCorner ?? Vector3.negativeInfinity,
                CurrentEftPathTarget = targetPoint ?? Vector3.negativeInfinity,
            };
        }
    }

    public struct BotDebugQuestingInfoPacket : INetSerializable, IBotDebugQuestingInfo
    {
        public int NetId { get; private set; }
        public bool AllowedToQuest { get; private set; }
        public bool IsQuesting { get; private set; }
        public string QuestName { get; private set; }
        public string QuestObjectiveName { get; private set; }
        public string QuestObjectiveStepName { get; private set; }
        public EJobAssignmentStatus QuestStatus { get; private set; }
        public EBotQuestingDecision CurrentDecision { get; private set; }
        public UnityEngine.AI.NavMeshPathStatus PathStatus { get; private set; }
        public bool HasQuestingPath { get; private set; }
        public ClassVector3 CurrentQuestingPathTarget { get; private set; }
        public Vector3[] CurrentQuestingPath { get; private set; }

        public void Deserialize(NetDataReader reader)
        {
            NetId = reader.GetInt();
            AllowedToQuest = reader.GetBool();
            IsQuesting = reader.GetBool();
            QuestName = reader.GetString();
            QuestObjectiveName = reader.GetString();
            QuestObjectiveStepName = reader.GetString();
            QuestStatus = reader.GetEnum<EJobAssignmentStatus>();
            CurrentDecision = reader.GetEnum<EBotQuestingDecision>();
            PathStatus = reader.GetEnum<UnityEngine.AI.NavMeshPathStatus>();
            HasQuestingPath = reader.GetBool();
            CurrentQuestingPathTarget = reader.GetClassVector3();
            CurrentQuestingPath = reader.GetUnmanagedArray<Vector3>();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(NetId);
            writer.Put(AllowedToQuest);
            writer.Put(IsQuesting);
            writer.Put(QuestName);
            writer.Put(QuestObjectiveName);
            writer.Put(QuestObjectiveStepName);
            writer.PutEnum(QuestStatus);
            writer.PutEnum(CurrentDecision);
            writer.PutEnum(PathStatus);
            writer.Put(HasQuestingPath);
            writer.PutClassVector3(CurrentQuestingPathTarget);
            writer.PutUnmanagedArray(CurrentQuestingPath);
        }

        public static BotDebugQuestingInfoPacket Create(FikaBot bot)
        {
            bool allowedToQuest = bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision);
            bool isQuesting = bot.AIData.BotOwner.IsQuesting(out botObjectiveManager);
            bool hasQuestingPath = bot.AIData.BotOwner.HasQuestingPath(out Vector3? targetPosition, out Vector3[] questingPathCorners);

            return new BotDebugQuestingInfoPacket()
            {
                NetId = bot.NetId,
                AllowedToQuest = allowedToQuest,
                IsQuesting = isQuesting,
                QuestName = botObjectiveManager?.CurrentAssignment?.QuestAssignment?.ToString() ?? string.Empty,
                QuestObjectiveName = botObjectiveManager?.CurrentAssignment?.QuestObjectiveAssignment?.ToString() ?? string.Empty,
                QuestObjectiveStepName = botObjectiveManager?.CurrentAssignment?.QuestObjectiveStepAssignment?.ToString() ?? string.Empty,
                QuestStatus = botObjectiveManager?.CurrentAssignment?.Status ?? EJobAssignmentStatus.NotStarted,
                CurrentDecision = currentDecision,
                PathStatus = botObjectiveManager?.BotPath?.Status ?? UnityEngine.AI.NavMeshPathStatus.PathComplete,
                HasQuestingPath = hasQuestingPath,
                CurrentQuestingPathTarget = targetPosition ?? Vector3.negativeInfinity,
                CurrentQuestingPath = questingPathCorners,
            };
        }
    }
}
