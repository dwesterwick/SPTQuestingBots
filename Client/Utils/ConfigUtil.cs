using Comfort.Common;
using Newtonsoft.Json;
using QuestingBots.Configuration;
using QuestingBots.Helpers;
using SPT.Common.Http;
using SPT.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuestingBots.Utils
{
    internal class ConfigUtil
    {
        private Configuration.ModConfig? _currentConfig;
        public Configuration.ModConfig CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    _currentConfig = GetConfig();
                }

                return _currentConfig!;
            }
        }

        private double _currentUsecChance = float.NaN;
        public double CurrentUSECChance
        {
            get
            {
                if (double.IsNaN(_currentUsecChance))
                {
                    _currentUsecChance = GetUSECChance();
                }

                return _currentUsecChance;
            }
        }

        private Dictionary<string, Configuration.ScavRaidSettingsConfig> _scavRaidSettings = null!;
        public Dictionary<string, Configuration.ScavRaidSettingsConfig> ScavRaidSettings
        {
            get
            {
                if (_scavRaidSettings == null)
                {
                    _scavRaidSettings = GetScavRaidSettings();
                }

                return _scavRaidSettings;
            }
        }

        private JsonSerializerSettings _serializerSettings = null!;
        public JsonSerializerSettings SerializerSettings
        {
            get
            {
                if ( _serializerSettings == null)
                {
                    _serializerSettings = findSerializerSettings();
                }

                return _serializerSettings;
            }
        }

        public ConfigUtil() { }

        private JsonSerializerSettings findSerializerSettings()
        {
            string fieldName = "SerializerSettings";
            Type targetType = Helpers.TarkovTypeHelpers.FindTargetTypeByField(fieldName);
            Singleton<LoggingUtil>.Instance.LogInfo("Found type for " + fieldName + ": " + targetType.FullName, true);

            JsonSerializerSettings? jsonSerializerSettings = targetType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static).GetValue(null) as JsonSerializerSettings;
            if (jsonSerializerSettings == null)
            {
                throw new InvalidOperationException("Cannot find EFT JsonSerializerSettings type");
            }

            return jsonSerializerSettings;
        }

        private Configuration.ModConfig GetConfig()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetConfig");
            string errorMessage = "!!!!! Cannot retrieve config.json data from the server. The mod will not work properly! !!!!!";
            string json = GetJson(routeName, errorMessage);

            if (!TryDeserializeObject(json, errorMessage, out Configuration.ModConfig _config))
            {
                return null!;
            }

            return _config;
        }

        private double GetUSECChance()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetUSECChance");
            string errorMessage = "Cannot retrieve chance to make PMCs USECs.";
            string json = GetJson(routeName, errorMessage);

            TryDeserializeObject(json, errorMessage, out Configuration.ServerResponses.ServerResponse _usecChanceResponse);
            double userChance = double.Parse(_usecChanceResponse.Data.ToString());

            return userChance;
        }

        private Dictionary<string, Configuration.ScavRaidSettingsConfig> GetScavRaidSettings()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetScavRaidSettings");
            string errorMessage = "Cannot read scav-raid settings.";
            string json = GetJson(routeName, errorMessage);

            TryDeserializeObject(json, errorMessage, out Dictionary<string, Configuration.ScavRaidSettingsConfig> _response);
            return _response;
        }

        public RawQuestClass[] GetAllQuestTemplates()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetAllQuestTemplates");
            string errorMessage = "Cannot read quest templates.";
            string json = GetJson(routeName, errorMessage);

            TryDeserializeObject(json, errorMessage, out RawQuestClass[] _templates);
            return _templates;
        }

        public Dictionary<string, Dictionary<string, object>> GetEFTQuestSettings()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetEFTQuestSettings");
            string errorMessage = "Cannot retrieve EFT quest settings.";
            string json = GetJson(routeName, errorMessage);

            TryDeserializeObject(json, errorMessage, out Dictionary<string, Dictionary<string, object>> _settings);
            return _settings;
        }

        public Dictionary<string, ZoneAndItemPositionInfoConfig> GetZoneAndItemPositions()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetZoneAndItemQuestPositions");
            string errorMessage = "Cannot retrieve positions for quest zones and items.";
            string json = GetJson(routeName, errorMessage);

            TryDeserializeObject(json, errorMessage, out Dictionary<string, ZoneAndItemPositionInfoConfig> _positions);
            return _positions;
        }

        public IEnumerable<Models.Questing.Quest> GetCustomQuests(string locationID)
        {
            Models.Questing.Quest[] standardQuests = new Models.Questing.Quest[0];
            string filename = Singleton<LoggingUtil>.Instance.LoggingPath + "..\\Quests\\Standard\\" + locationID + ".json";
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
            filename = Singleton<LoggingUtil>.Instance.LoggingPath + "..\\Quests\\Custom\\" + locationID + ".json";
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

        private string GetJson(string endpoint, string errorMessage)
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

        private bool TryDeserializeObject<T>(string json, string errorMessage, out T obj)
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
                    Configuration.ServerResponses.ServerResponse? serverResponse = JsonConvert.DeserializeObject<Configuration.ServerResponses.ServerResponse>(json);
                    if (serverResponse == null)
                    {
                        throw new System.Net.WebException("Could not deserialize server response");
                    }

                    if (serverResponse?.StatusCode != System.Net.HttpStatusCode.OK)
                    {
                        throw new System.Net.WebException("Could not retrieve configuration settings from the server. Response: " + serverResponse!.StatusCode.ToString());
                    }
                }

                obj = JsonConvert.DeserializeObject<T>(json, SerializerSettings)!;

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
