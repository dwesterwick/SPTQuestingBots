using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components.Spawning;

namespace QuestingBots
{
    internal static class QuestingBotsExternal
    {
        public static int GetRemainingBotGenerators()
        {
            return BotGenerator.RemainingBotGenerators;
        }

        public static int GetCurrentBotGeneratorProgress()
        {
            return BotGenerator.CurrentBotGeneratorProgress;
        }

        public static string GetCurrentBotGeneratorType()
        {
            return BotGenerator.CurrentBotGeneratorType;
        }

        public static string GetCurrentDecision(BotOwner bot)
        {
            BotQuestingDecision defaultDecision = BotQuestingDecision.None;

            if ((bot == null) || (bot.BotState != EBotState.Active) || bot.IsDead)
            {
                return defaultDecision.ToString();
            }

            BotMonitorController botMonitor = bot.GetPlayer.gameObject.GetOrAddComponent<BotMonitorController>();
            if (botMonitor == null)
            {
                return defaultDecision.ToString();
            }

            return botMonitor.CurrentDecision.ToString();
        }
    }
}
