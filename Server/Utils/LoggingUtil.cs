using QuestingBots.Helpers;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Utils
{
    [Injectable(InjectionType.Singleton)]
    public class LoggingUtil(ISptLogger<QuestingBots_Server> logger, ConfigUtil _configUtil)
    {
        public void Debug(string message)
        {
            if (!_configUtil.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

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