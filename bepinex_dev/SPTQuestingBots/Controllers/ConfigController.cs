using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.Controllers
{
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null;
        public static Dictionary<string, Configuration.ScavRaidSettingsConfig> ScavRaidSettings = null;
        public static string LoggingPath { get; private set; } = null;

        public static Configuration.ModConfig GetConfig()
        {
            string errorMessage = "!!!!! Cannot retrieve config.json data from the server. The mod will not work properly! !!!!!";
            string json = GetJson("/QuestingBots/GetConfig", errorMessage);

            if (!TryDeserializeObject(json, errorMessage, out Configuration.ModConfig _config))
            {
                return null;
            }
            Config = _config;

            return Config;
        }

        public static void AdjustPMCConversionChances(float factor, bool verify)
        {
            GetJson("/QuestingBots/AdjustPMCConversionChances/" + factor + "/" + verify.ToString(), "Could not adjust PMC conversion chances");
        }

        public static void AdjustPScavChance(float timeRemainingFactor, bool preventPScav)
        {
            double factor = preventPScav ? 0 : InterpolateForFirstCol(Config.AdjustPScavChance.ChanceVsTimeRemainingFraction, timeRemainingFactor);

            GetJson("/QuestingBots/AdjustPScavChance/" + factor, "Could not adjust PScav conversion chance");
        }

        public static void ReportInfoToServer(string message)
        {
            Aki.Common.Utils.ServerLog.Info("Questing Bots", message);
        }

        public static void ReportWarningToServer(string message)
        {
            Aki.Common.Utils.ServerLog.Warn("Questing Bots", message);
        }

        public static void ReportErrorToServer(string message)
        {
            Aki.Common.Utils.ServerLog.Error("Questing Bots", message);
        }

        public static string GetLoggingPath()
        {
            if (LoggingPath != null)
            {
                return LoggingPath;
            }

            string qbDir = AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/DanW-SPTQuestingBots/log/";
            LoggingPath = qbDir;

            LoggingController.LogInfo("Logging path: " + LoggingPath);

            return LoggingPath;
        }

        public static Dictionary<string, Configuration.ScavRaidSettingsConfig> GetScavRaidSettings()
        {
            if (ScavRaidSettings != null)
            {
                return ScavRaidSettings;
            }

            string errorMessage = "Cannot read scav-raid settings.";
            string json = GetJson("/QuestingBots/GetScavRaidSettings", errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.ScavRaidSettingsResponse _response);
            ScavRaidSettings = _response.Maps;

            return ScavRaidSettings;
        }

        public static RawQuestClass[] GetAllQuestTemplates()
        {
            string errorMessage = "Cannot read quest templates.";
            string json = GetJson("/QuestingBots/GetAllQuestTemplates", errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.QuestDataConfig _templates);
            return _templates.Templates;
        }

        public static IEnumerable<Quest> GetCustomQuests(string locationID)
        {
            Quest[] standardQuests = new Quest[0];
            string filename = GetLoggingPath() + "..\\quests\\standard\\" + locationID + ".json";
            if (File.Exists(filename))
            {
                string errorMessage = "Cannot read standard quests for " + locationID;
                try
                {
                    string json = File.ReadAllText(filename);
                    TryDeserializeObject(json, errorMessage, out standardQuests);
                }
                catch (Exception e)
                {
                    LoggingController.LogError(e.Message);
                    LoggingController.LogError(e.StackTrace);
                    LoggingController.LogErrorToServerConsole(errorMessage);
                }
            }

            Quest[] customQuests = new Quest[0];
            filename = GetLoggingPath() + "..\\quests\\custom\\" + locationID + ".json";
            if (File.Exists(filename))
            {
                string errorMessage = "Cannot read custom quests for " + locationID;
                try
                {
                    string json = File.ReadAllText(filename);
                    TryDeserializeObject(json, errorMessage, out customQuests);
                }
                catch (Exception e)
                {
                    LoggingController.LogError(e.Message);
                    LoggingController.LogError(e.StackTrace);
                    LoggingController.LogErrorToServerConsole(errorMessage);
                }
            }

            return standardQuests.Concat(customQuests);
        }

        public static string GetJson(string endpoint, string errorMessage)
        {
            string json = null;
            Exception lastException = null;

            // Sometimes server requests fail, and nobody knows why. If this happens, retry a few times.
            for (int i = 0; i < 5; i++)
            {
                try
                {
                    json = RequestHandler.GetJson(endpoint);
                }
                catch (Exception e)
                {
                    lastException = e;

                    LoggingController.LogWarning("Could not get data for " + endpoint);
                }

                if (json != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            if (json == null)
            {
                LoggingController.LogError(lastException.Message);
                LoggingController.LogError(lastException.StackTrace);
                LoggingController.LogErrorToServerConsole(errorMessage);
            }

            return json;
        }

        public static bool TryDeserializeObject<T>(string json, string errorMessage, out T obj)
        {
            try
            {
                if (json.Length == 0)
                {
                    throw new InvalidCastException("Could deserialize an empty string to an object of type " + typeof(T).FullName);
                }

                // Check if the server failed to provide a valid response
                if (!json.StartsWith("["))
                {
                    ServerResponseError serverResponse = JsonConvert.DeserializeObject<ServerResponseError>(json);
                    if (serverResponse?.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new System.Net.WebException("Could not retrieve configuration settings from the server. Response: " + serverResponse.StatusCode.ToString());
                    }
                }

                obj = JsonConvert.DeserializeObject<T>(json, GClass1448.SerializerSettings);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);
                LoggingController.LogErrorToServerConsole(errorMessage);
            }

            obj = default(T);
            if (obj == null)
            {
                obj = (T)Activator.CreateInstance(typeof(T));
            }

            return false;
        }

        public static double InterpolateForFirstCol(double[][] array, double value)
        {
            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            if (value <= array[0][0])
            {
                return array[0][1];
            }

            for (int i = 1; i < array.Length; i++)
            {
                if (array[i][0] >= value)
                {
                    if (array[i][0] - array[i - 1][0] == 0)
                    {
                        return array[i][1];
                    }

                    return array[i - 1][1] + (value - array[i - 1][0]) * (array[i][1] - array[i - 1][1]) / (array[i][0] - array[i - 1][0]);
                }
            }

            return array.Last()[1];
        }
    }
}
