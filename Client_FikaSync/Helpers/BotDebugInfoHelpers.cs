using EFT;
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
    }
}
