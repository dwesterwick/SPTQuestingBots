using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class BotInfoHelpers
    {
        public static IEnumerable<IPlayer> NonAIPlayers(this IEnumerable<IPlayer> players)
        {
            foreach (Player player in players)
            {
                if (player.IsAI)
                {
                    continue;
                }

                yield return player;
            }
        }

        public static IEnumerable<IPlayer> AlivePlayers(this IEnumerable<IPlayer> players)
        {
            foreach (Player player in players)
            {
                if (!player.HealthController.IsAlive)
                {
                    continue;
                }

                yield return player;
            }
        }

        public static IEnumerable<BotOwner> ActiveBots(this IEnumerable<BotOwner> bots)
        {
            foreach (BotOwner bot in bots)
            {
                if (bot.BotState != EBotState.Active)
                {
                    continue;
                }

                yield return bot;
            }
        }
    }
}
