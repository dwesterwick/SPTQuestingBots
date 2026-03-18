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
using QuestingBots.Configuration;
using QuestingBots.Helpers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Controllers
{
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null!;
        public static Dictionary<string, Configuration.ScavRaidSettingsConfig> ScavRaidSettings { get; private set; } = null!;
        public static float USECChance { get; private set; } = float.NaN;
        public static string ModPathRelative { get; } = "/BepInEx/plugins/DanW-QuestingBots";
        public static string LoggingPath { get; private set; } = null!;

        private static JsonSerializerSettings serializerSettings = null!;

        public static Configuration.ModConfig GetConfig()
        {
            findSerializerSettings();

            string errorMessage = "!!!!! Cannot retrieve config.json data from the server. The mod will not work properly! !!!!!";
            string json = GetJson("/QuestingBots/GetConfig", errorMessage);

            if (!TryDeserializeObject(json, errorMessage, out Configuration.ModConfig _config))
            {
                return null!;
            }
            Config = _config;

            return Config;
        }

        private static void findSerializerSettings()
        {
            if (serializerSettings != null)
            {
                return;
            }

            string fieldName = "SerializerSettings";
            Type targetType = Helpers.TarkovTypeHelpers.FindTargetTypeByField(fieldName);
            Singleton<LoggingUtil>.Instance.LogInfo("Found type for " + fieldName + ": " + targetType.FullName, true);

            JsonSerializerSettings? jsonSerializerSettings = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static).GetValue(null) as JsonSerializerSettings;
            if (jsonSerializerSettings != null)
            {
                serializerSettings = jsonSerializerSettings;
            }
        }

        public static void AdjustPScavChance(float timeRemainingFactor, bool preventPScav)
        {
            double factor = preventPScav ? 0 : Config.AdjustPScavChance.ChanceVsTimeRemainingFraction.InterpolateForFirstCol(timeRemainingFactor);

            GetJson("/QuestingBots/AdjustPScavChance/" + factor, "Could not adjust PScav conversion chance");
        }

        public static string GetLoggingPath()
        {
            if (LoggingPath != null)
            {
                return LoggingPath;
            }

            LoggingPath = AppDomain.CurrentDomain.BaseDirectory + ModPathRelative + "/log/";
            Singleton<LoggingUtil>.Instance.LogInfo("Logging path: " + LoggingPath);

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
                    Singleton<LoggingUtil>.Instance.LogError(e.Message);
                    Singleton<LoggingUtil>.Instance.LogError(e.StackTrace);
                    Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(errorMessage);
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
                    Singleton<LoggingUtil>.Instance.LogError(e.Message);
                    Singleton<LoggingUtil>.Instance.LogError(e.StackTrace);
                    Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(errorMessage);
                }
            }

            return standardQuests.Concat(customQuests);
        }

        public static string GetJson(string endpoint, string errorMessage)
        {
            string json = null!;
            Exception lastException = null!;

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

                    Singleton<LoggingUtil>.Instance.LogWarning("Could not get data for " + endpoint);
                }

                if (json != null)
                {
                    break;
                }

                Thread.Sleep(100);
            }

            if (json == null)
            {
                Singleton<LoggingUtil>.Instance.LogError(lastException.Message);
                Singleton<LoggingUtil>.Instance.LogError(lastException.StackTrace);
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(errorMessage);
            }

            return json!;
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
                    ServerResponseError? serverResponse = JsonConvert.DeserializeObject<ServerResponseError>(json);
                    if (serverResponse == null)
                    {
                        throw new System.Net.WebException("Could not deserialize server response");
                    }
                    
                    if (serverResponse?.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new System.Net.WebException("Could not retrieve configuration settings from the server. Response: " + serverResponse!.StatusCode.ToString());
                    }
                }

                obj = JsonConvert.DeserializeObject<T>(json, serializerSettings)!;

                return true;
            }
            catch (Exception e)
            {
                Singleton<LoggingUtil>.Instance.LogError(e.Message);
                Singleton<LoggingUtil>.Instance.LogError(e.StackTrace);
                Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(errorMessage);
            }

            obj = default(T)!;
            if (obj == null)
            {
                obj = (T)Activator.CreateInstance(typeof(T));
            }

            return false;
        }
    }
}
