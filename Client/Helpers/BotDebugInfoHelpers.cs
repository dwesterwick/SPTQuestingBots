using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Helpers
{
    public static class BotDebugInfoHelpers
    {
        public static bool CanEnableBotDebugDataBroadcasting()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Bot debug data broadcasting disabled because debug is disabled in the server config");
                return false;
            }

            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Debug.BroadcastDebugDataToFikaClients)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Bot debug data broadcasting disabled in the server config");
                return false;
            }

            return true;
        }

        public static bool IsInitialized(this BotOwner bot)
        {
            BotObjectiveManager? objectiveManager = bot.GetObjectiveManager();
            if (objectiveManager != null)
            {
                return objectiveManager.IsInitialized;
            }

            return false;
        }

        public static bool AllowedToQuest(this BotOwner bot, out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision)
        {
            currentDecision = EBotQuestingDecision.Inactive;

            botObjectiveManager = bot.GetObjectiveManager();
            if (botObjectiveManager == null)
            {
                return false;
            }

            BotQuestingDecisionMonitor? questingDecisionMonitor = botObjectiveManager.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
            if (questingDecisionMonitor == null)
            {
                return false;
            }

            currentDecision = questingDecisionMonitor.CurrentDecision;
            return botObjectiveManager.IsQuestingAllowed;
        }

        public static bool IsQuesting(this BotOwner bot, out BotObjectiveManager? botObjectiveManager)
        {
            if (!bot.AllowedToQuest(out botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null))
            {
                return false;
            }

            if (botObjectiveManager.CurrentAssignment == null)
            {
                return false;
            }

            BotQuestingDecisionMonitor? questingDecisionMonitor = botObjectiveManager.BotMonitor?.GetMonitor<BotQuestingDecisionMonitor>();
            if (questingDecisionMonitor == null)
            {
                return false;
            }

            bool hasBoss = bot.HasBoss(out int _);
            return !hasBoss || questingDecisionMonitor.MustQuestBeforeFollowing;
        }

        public static bool HasQuestingPath(this BotOwner bot, out Vector3? targetPosition, out Vector3[] questingPathCorners)
        {
            if (!bot.AllowedToQuest(out BotObjectiveManager? botObjectiveManager, out EBotQuestingDecision currentDecision) || (botObjectiveManager == null))
            {
                targetPosition = null;
                questingPathCorners = Array.Empty<Vector3>();
                return false;
            }

            targetPosition = botObjectiveManager.BotPath.TargetPosition;
            questingPathCorners = botObjectiveManager.BotPath.Corners;
            return true;
        }

        public static bool HasEftPath(this BotOwner bot, out Vector3? targetPoint, out Vector3? currentCorner)
        {
            if (bot.Mover?.ActualPathController?.HavePath != true)
            {
                targetPoint = null;
                currentCorner = null;

                return false;
            }

            targetPoint = bot.Mover.ActualPathController.TargetPoint;
            currentCorner = bot.Mover.ActualPathController.CurrentCorner();

            return true;
        }

        public static int[] EnemyPlayerIdsArray(this BotOwner bot)
        {
            if (bot.EnemiesController?.EnemyInfos == null)
            {
                return new int[0];
            }

            return bot.EnemiesController.EnemyInfos
                .SelectNonAlloc(info => info.Value.EnemyPlayerId)
                .ToArray();
        }

        public static bool HasCurrentEnemy(this BotOwner bot, out int enemyPlayerId)
        {
            enemyPlayerId = -1;
            Player? enemyPlayer = bot.Memory.GoalEnemy?.GroupInfo?.Player;
            if (enemyPlayer == null)
            {
                return false;
            }

            enemyPlayerId = enemyPlayer.GetPlayer.Id;
            return enemyPlayer.HealthController.IsAlive;
        }

        public static bool HasBoss(this BotOwner bot, out int bossPlayerId)
        {
            bossPlayerId = -1;
            BotOwner? boss = BotHiveMindMonitor.GetGroupLeader(bot);
            if (boss == null)
            {
                return false;
            }

            bossPlayerId = boss.GetPlayer.Id;
            return boss.HealthController.IsAlive;
        }
    }
}
