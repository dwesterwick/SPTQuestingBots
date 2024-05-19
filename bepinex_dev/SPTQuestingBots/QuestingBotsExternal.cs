using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBots.Components.Spawning;

namespace SPTQuestingBots
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
    }
}
