using Comfort.Common;
using Newtonsoft.Json;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using System;
using System.IO;

namespace QuestingBots.Utils
{
    internal class LoggingUtil
    {
        public const string MOD_RELATIVE_PATH = "/BepInEx/plugins/QuestingBots";

        private BepInEx.Logging.ManualLogSource _logger;

        private string _loggingPath = null!;
        public string LoggingPath
        {
            get
            {
                if (_loggingPath == null)
                {
                    _loggingPath = GetLoggingPath();
                }

                return _loggingPath;
            }
        }

        private string GetLoggingPath()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + MOD_RELATIVE_PATH + "/log/";
            LogInfo("Logging path: " + path);

            return path;
        }

        public LoggingUtil(BepInEx.Logging.ManualLogSource logger)
        {
            _logger = logger;
        }

        public void LogDebug(string message)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogDebug(message);
        }

        public void LogInfo(string message, bool alwaysShow = false)
        {
            if (!alwaysShow && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogInfo(message);
        }

        public void LogWarning(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogWarning(message);
        }

        public void LogError(string message, bool onlyForDebug = false)
        {
            if (onlyForDebug && !Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            _logger.LogError(message);
        }

        public void LogDebugToServerConsole(string message)
        {
            LogDebug(message);
            SPT.Common.Utils.ServerLog.Debug(ModInfo.MODNAME, message);
        }

        public void LogInfoToServerConsole(string message)
        {
            LogInfo(message);
            SPT.Common.Utils.ServerLog.Info(ModInfo.MODNAME, message);
        }

        public void LogWarningToServerConsole(string message)
        {
            LogWarning(message);
            SPT.Common.Utils.ServerLog.Warn(ModInfo.MODNAME, message);
        }

        public void LogErrorToServerConsole(string message)
        {
            LogError(message);
            SPT.Common.Utils.ServerLog.Error(ModInfo.MODNAME, message);
        }

        public void CreateLogFile(string logName, string filename, string content)
        {
            try
            {
                if (!Directory.Exists(Singleton<LoggingUtil>.Instance.LoggingPath))
                {
                    Directory.CreateDirectory(Singleton<LoggingUtil>.Instance.LoggingPath);
                }

                File.WriteAllText(filename, content);

                LogDebug("Writing " + logName + " log file...done.");
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                LogError("Writing " + logName + " log file...failed!");
                LogError(e.ToString());
            }
        }

        public void AppendQuestLocationToCurrentLogFile(string filename, Models.Questing.StoredQuestLocation location)
        {
            try
            {
                string content = JsonConvert.SerializeObject(location, Formatting.Indented);

                if (!Directory.Exists(Singleton<LoggingUtil>.Instance.LoggingPath))
                {
                    Directory.CreateDirectory(Singleton<LoggingUtil>.Instance.LoggingPath);
                }

                if (File.Exists(filename))
                {
                    content = ",\n" + content;
                }

                File.AppendAllText(filename, content);

                LogInfo("Appended custom quest location: " + location.Name + " at " + location.Position.ToString());
            }
            catch (Exception e)
            {
                e.Data.Add("Filename", filename);
                e.Data.Add("LocationName", location.Name);
                LogError("Could not create custom quest location for " + location.Name);
                LogError(e.ToString());
            }
        }
    }
}