using EFT;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.Helpers
{
    internal static class BotDebugInfoHelpers
    {
        internal static bool IsInitialized(this BotOwner bot)
        {
            BotObjectiveManager? objectiveManager = bot.GetObjectiveManager();
            if (objectiveManager != null)
            {
                return objectiveManager.IsInitialized;
            }

            return false;
        }

        internal static int[] EnemyPlayerIdsArray(this BotOwner bot)
        {
            if (bot.EnemiesController?.EnemyInfos == null)
            {
                return new int[0];
            }

            return bot.EnemiesController.EnemyInfos
                .SelectNonAlloc(info => info.Value.EnemyPlayerId)
                .ToArray();
        }

        internal static bool HasCurrentEnemy(this BotOwner bot, out int enemyPlayerId)
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

        internal static bool HasBoss(this BotOwner bot, out int bossPlayerId)
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
