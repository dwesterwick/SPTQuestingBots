using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Common.Http;
using Newtonsoft.Json;

namespace QuestingBots.Controllers
{
    public static class ConfigController
    {
        public static Configuration.ModConfig Config { get; private set; } = null;

        public static Configuration.ModConfig GetConfig()
        {
            string errorMessage = "!!!!! Cannot retrieve config.json data from the server. The mod will not work properly! !!!!!";
            string json = RequestHandler.GetJson("/QuestingBots/GetConfig");

            TryDeserializeObject(json, errorMessage, out Configuration.ModConfig _config);
            Config = _config;

            return Config;
        }

        public static void AdjustPMCConversionChances(float factor)
        {
            RequestHandler.GetJson("/QuestingBots/AdjustPMCConversionChances/" + factor);
        }

        public static void ReportError(string errorMessage)
        {
            RequestHandler.GetJson("/QuestingBots/ReportError/" + errorMessage);
        }

        public static RawQuestClass[] GetAllQuestTemplates()
        {
            string errorMessage = "Cannot read quest templates.";
            string json = RequestHandler.GetJson("/QuestingBots/GetAllQuestTemplates");

            TryDeserializeObject(json, errorMessage, out Configuration.QuestTemplatesConfig _templates);
            return _templates.Quests;
        }

        public static bool TryDeserializeObject<T>(string json, string errorMessage, out T obj)
        {
            try
            {
                if (json.Length == 0)
                {
                    throw new InvalidCastException("Could deserialize an empty string to an object of type " + typeof(T).FullName);
                }

                obj = JsonConvert.DeserializeObject<T>(json, GClass1442.SerializerSettings);

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
    }
}
