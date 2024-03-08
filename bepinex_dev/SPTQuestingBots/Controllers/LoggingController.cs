using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.Controllers
{
    public static class LoggingController
    {
        public static BepInEx.Logging.ManualLogSource Logger { get; set; } = null;

        public static string GetText(this IEnumerable<Player> players) => string.Join(",", players.Select(b => b?.GetText()));
        public static string GetText(this IEnumerable<IPlayer> players) => string.Join(",", players.Select(b => b?.GetText()));
        public static string GetText(this IEnumerable<BotOwner> bots) => string.Join(",", bots.Select(b => b?.GetText()));

        public static string GetText(this BotOwner bot)
        {
            if (bot == null)
            {
                return "[NULL BOT]";
            }

            return bot.GetPlayer.GetText();
        }
        
        public static string GetText(this Player player)
        {
            if (player == null)
            {
                return "[NULL BOT]";
            }

            return player.Profile.Nickname + " (Name: " + player.name + ", Level: " + player.Profile.Info.Level.ToString() + ")";
        }

        public static string GetText(this IPlayer player)
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

        public static void LogInfo(string message)
        {
            if (!ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogInfo(message);
        }

        public static void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogWarning(message);
        }

        public static void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !ConfigController.Config.Debug.Enabled)
            {
                return;
            }

            Logger.LogError(message);
        }

        public static void LogErrorToServerConsole(string message)
        {
            LogError(message);
            ConfigController.ReportError(message);
        }

        public static void CreateLogFile(string logName, string filename, string content)
        {
            try
            {
                if (!Directory.Exists(ConfigController.LoggingPath))
                {
                    Directory.CreateDirectory(ConfigController.LoggingPath);
                }

                File.WriteAllText(filename, content);

                LogInfo("Writing " + logName + " log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LogError("Writing " + logName + " log file...failed!");
                LogError(e.ToString());
            }
        }

        private static string GetMessagePrefix(char messageType)
        {
            return "[" + messageType + "] " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": ";
        }
    }
}
