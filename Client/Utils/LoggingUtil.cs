namespace QuestingBots.Utils
{
    internal class LoggingUtil
    {
        private BepInEx.Logging.ManualLogSource _logger;

        public LoggingUtil(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }

        public void LogInfo(string message)
        {
            _logger.LogInfo(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message)
        {
            _logger.LogError(message);
        }
    }
}