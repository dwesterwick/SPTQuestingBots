using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public class LocalBotDebugInfoSource : IBotDebugInfoSource
    {
        public BotOwner Bot { get; private set; }

        public int NetId => Bot.Id;

        public int BotId => Bot.Id;
        public string BotName => Bot.name;
        public bool IsInitialized => Bot.AIData.BotOwner.IsInitialized();
        public EBotType BotType => Controllers.BotRegistrationManager.GetBotType(Bot.AIData.BotOwner);
        public string BaseBrainName => Bot.AIData.BotOwner.Brain.BaseBrain.ShortName();

        public bool HasCurrentData => Bot != null;
        public EBotState BotState => Bot.AIData.BotOwner.BotState;
        public bool IsSleeping => BotRegistrationManager.IsBotSleeping(Bot.Profile.Id);
        public int[] EnemyPlayerIds => Bot.AIData.BotOwner.EnemyPlayerIdsArray();
        public string ActiveLayerName => Bot.AIData.BotOwner.GetActiveLayerName();
        public string ActiveLogicName => Bot.AIData.BotOwner.GetActiveLogicName();
        public bool HasCurrentEnemy => Bot.AIData.BotOwner.HasCurrentEnemy(out int currentEnemyPlayerId);
        public int CurrentEnemyPlayerId
        {
            get
            {
                if (!Bot.AIData.BotOwner.HasCurrentEnemy(out int currentEnemyPlayerId)) { return -1; }
                return currentEnemyPlayerId;
            }
        }
        public bool HasBoss => Bot.AIData.BotOwner.HasBoss(out int bossPlayerId);
        public int BossPlayerId
        {
            get
            {
                if (!Bot.AIData.BotOwner.HasBoss(out int bossPlayerId)) { return -1; }
                return bossPlayerId;
            }
        }
        public bool HasEftPath => Bot.AIData.BotOwner.HasEftPath(out Vector3? targetPoint, out Vector3? currentCorner);
        public ClassVector3 CurrentEftPathCorner
        {
            get
            {
                if (!Bot.AIData.BotOwner.HasEftPath(out Vector3? targetPoint, out Vector3? currentCorner) || (currentCorner == null)) { return Vector3.negativeInfinity; }
                return currentCorner;
            }
        }
        public ClassVector3 CurrentEftPathTarget
        {
            get
            {
                if (!Bot.AIData.BotOwner.HasEftPath(out Vector3? targetPoint, out Vector3? currentCorner) || (targetPoint == null)) { return Vector3.negativeInfinity; }
                return targetPoint;
            }
        }

        public bool HasQuestingData => Bot != null;
        public bool AllowedToQuest => Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision);
        public bool IsQuesting => Bot.AIData.BotOwner.IsQuesting(out BotObjectiveManager? botObjectiveManager);
        public string QuestName
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null)) { return string.Empty; }
                return botObjectiveManager.CurrentAssignment?.QuestAssignment?.ToString() ?? string.Empty;
            }
        }
        public string QuestObjectiveName
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null)) { return string.Empty; }
                return botObjectiveManager.CurrentAssignment?.QuestObjectiveAssignment?.ToString() ?? string.Empty;
            }
        }
        public string QuestObjectiveStepName
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null)) { return string.Empty; }
                return botObjectiveManager.CurrentAssignment?.QuestObjectiveStepAssignment?.ToString() ?? string.Empty;
            }
        }
        public EJobAssignmentStatus QuestStatus
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null)) { return EJobAssignmentStatus.NotStarted; }
                return botObjectiveManager.CurrentAssignment?.Status ?? EJobAssignmentStatus.NotStarted;
            }
        }
        public EBotQuestingDecision CurrentDecision
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision)) { return EBotQuestingDecision.Inactive; }
                return currentDecision;
            }
        }
        public UnityEngine.AI.NavMeshPathStatus PathStatus
        {
            get
            {
                if (!Bot.AIData.BotOwner.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null)) { return UnityEngine.AI.NavMeshPathStatus.PathComplete; }
                return botObjectiveManager.BotPath.Status;
            }
        }
        public bool HasQuestingPath => Bot.AIData.BotOwner.HasQuestingPath(out Vector3? targetPosition, out Vector3[] questingPathCorners);
        public ClassVector3 CurrentQuestingPathTarget
        {
            get
            {
               if (!Bot.AIData.BotOwner.HasQuestingPath(out Vector3? targetPosition, out Vector3[] questingPathCorners) || (targetPosition == null)) { return Vector3.negativeInfinity; }
                return targetPosition;
            }
        }
        public Vector3[] CurrentQuestingPath
        {
            get
            {
                if (!Bot.AIData.BotOwner.HasQuestingPath(out Vector3? targetPosition, out Vector3[] questingPathCorners)) { return Array.Empty<Vector3>(); }
                return questingPathCorners;
            }
        }

        public LocalBotDebugInfoSource(BotOwner bot)
        {
            Bot = bot;
        }
    }
}
