using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LoggingUtil(ISptLogger<QuestingBots_Server> logger)
    {
        public void Debug(string message)
        {
            logger.Debug(GetLogPrefix() + message);
        }

        public void Info(string message)
        {
            logger.Info(GetLogPrefix() + message);
        }

        public void Warning(string message)
        {
            logger.Warning(GetLogPrefix() + message);
        }

        public void Error(string message)
        {
            logger.Error(GetLogPrefix() + message);
        }

        private string GetLogPrefix()
        {
            return $"[{ModInfo.MODNAME}] ";
        }
    }
}