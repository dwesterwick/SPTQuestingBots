using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SPT.Common.Http;
using Newtonsoft.Json;
using SPTQuestingBots.Configuration;
using SPTQuestingBots.Models;

namespace SPTQuestingBots.Controllers
{
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null;
        public static Dictionary<string, Configuration.ScavRaidSettingsConfig> ScavRaidSettings { get; private set; } = null;
        public static float USECChance { get; private set; } = float.NaN;
        public static string ModPathRelative { get; } = "/BepInEx/plugins/DanW-SPTQuestingBots";
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

        public static void AdjustPScavChance(float timeRemainingFactor, bool preventPScav)
        {
            double factor = preventPScav ? 0 : InterpolateForFirstCol(Config.AdjustPScavChance.ChanceVsTimeRemainingFraction, timeRemainingFactor);

            GetJson("/QuestingBots/AdjustPScavChance/" + factor, "Could not adjust PScav conversion chance");
        }

        public static void ReportInfoToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Info("Questing Bots", message);
        }

        public static void ReportWarningToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Warn("Questing Bots", message);
        }

        public static void ReportErrorToServer(string message)
        {
            SPT.Common.Utils.ServerLog.Error("Questing Bots", message);
        }

        public static string GetLoggingPath()
        {
            if (LoggingPath != null)
            {
                return LoggingPath;
            }

            LoggingPath = AppDomain.CurrentDomain.BaseDirectory + ModPathRelative + "/log/";
            LoggingController.LogInfo("Logging path: " + LoggingPath);

            return LoggingPath;
        }

        public static float GetUSECChance()
        {
            if (!float.IsNaN(USECChance))
            {
                return USECChance;
            }

            string errorMessage = "Cannot retrieve chance to make PMC's USEC's.";
            string json = GetJson("/QuestingBots/GetUSECChance", errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.USECChanceResponse _usecChance);
            USECChance = _usecChance.USECChance;
            return USECChance;
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

        public static Dictionary<string,Dictionary<string,object>> GetEFTQuestSettings()
        {
            string errorMessage = "Cannot retrieve EFT quest settings.";
            string json = GetJson("/QuestingBots/GetEFTQuestSettings", errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.QuestDataConfig _settings);
            return _settings.Settings;
        }

        public static Dictionary<string, ZoneAndItemPositionInfoConfig> GetZoneAndItemPositions()
        {
            string errorMessage = "Cannot retrieve positions for quest zones and items.";
            string json = GetJson("/QuestingBots/GetZoneAndItemQuestPositions", errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.QuestDataConfig _positions);
            return _positions.ZoneAndItemPositions;
        }

        public static IEnumerable<Models.Questing.Quest> GetCustomQuests(string locationID)
        {
            Models.Questing.Quest[] standardQuests = new Models.Questing.Quest[0];
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

            Models.Questing.Quest[] customQuests = new Models.Questing.Quest[0];
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

                obj = JsonConvert.DeserializeObject<T>(json, GClass1629.SerializerSettings);

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
            validateArray(array);

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

        public static double GetValueFromTotalChanceFraction(double[][] array, double fraction)
        {
            validateArray(array);

            if (array.Length == 1)
            {
                return array.Last()[1];
            }

            double chancesSum = array.Sum(x => x[1]);
            double targetCumulativeChances = chancesSum * fraction;

            int i = 0;
            double cumulativeChances = 0;
            while (i < array.Length)
            {
                cumulativeChances += array[i][1];

                if (cumulativeChances > targetCumulativeChances)
                {
                    return array[i][0];
                }

                i++;
            }

            return array.Last()[0];
        }

        private static void validateArray(double[][] array)
        {
            if (array.Length == 0)
            {
                throw new ArgumentOutOfRangeException("The array must have at least one row.");
            }

            if (array.Any(x => x.Length != 2))
            {
                throw new ArgumentOutOfRangeException("All rows in the array must have two columns.");
            }
        }
    }
}
