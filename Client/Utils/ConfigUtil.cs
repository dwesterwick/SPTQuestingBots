using Comfort.Common;
using Newtonsoft.Json;
using QuestingBots.Configuration;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace QuestingBots.Utils
{
    internal static class ConfigUtil
    {
        public static Configuration.ModConfig Config { get; private set; } = null!;
        public static Dictionary<string, Configuration.ScavRaidSettingsConfig> ScavRaidSettings { get; private set; } = null!;
        public static float USECChance { get; private set; } = float.NaN;

        private static Configuration.ModConfig? _currentConfig;
        public static Configuration.ModConfig CurrentConfig
        {
            get
            {
                if (_currentConfig == null)
                {
                    GetConfig();
                }

                return _currentConfig!;
            }
        }

        private static JsonSerializerSettings _serializerSettings = null!;
        public static JsonSerializerSettings SerializerSettings
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

        private static JsonSerializerSettings findSerializerSettings()
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

        private static void GetConfig()
        {
            string routeName = SharedRouterHelpers.GetRoutePath("GetConfig");

            string json = RequestHandler.GetJson(routeName);
            Configuration.ModConfig? configResponse = JsonConvert.DeserializeObject<Configuration.ModConfig>(json);
            if (configResponse == null)
            {
                throw new InvalidOperationException("Could not deserialize config file");
            }

            _currentConfig = configResponse;
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
