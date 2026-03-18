using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.Helpers
{
    internal static class LoggingHelpers
    {
        public static string GetText(this IEnumerable<Player> players) => string.Join(",", players.Select(b => b?.GetText()));
        public static string GetText(this IEnumerable<IPlayer> players) => string.Join(",", players.Select(b => b?.GetText()));
        public static string GetText(this IEnumerable<BotOwner> bots) => string.Join(",", bots.Select(b => b?.GetText()));

        public static string GetText(this BotOwner? bot)
        {
            if (bot == null)
            {
                return "[NULL BOT]";
            }

            return bot.GetPlayer.GetText();
        }

        public static string GetText(this Player? player)
        {
            if (player == null)
            {
                return "[NULL BOT]";
            }

            return player.Profile.Nickname + " (Name: " + player.name + ", Level: " + player.Profile.Info.Level.ToString() + ")";
        }

        public static string GetText(this IPlayer? player)
        {
            if (player == null)
            {
                return "[NULL BOT]";
            }

            return player.Profile.Nickname + " (Name: ???, Level: " + player.Profile.Info.Level.ToString() + ")";
        }

        public static string Abbreviate(this string fullID, int startChars = 5, int endChars = 5)
        {
            if (fullID.Length <= startChars + endChars + 3)
            {
                return fullID;
            }

            return fullID.Substring(0, startChars) + "..." + fullID.Substring(fullID.Length - endChars, endChars);
        }
    }
}
