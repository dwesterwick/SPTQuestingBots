using EFT;
using QuestingBots.Components;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
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
    }
}
