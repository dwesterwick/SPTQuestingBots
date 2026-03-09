using QuestingBots.Helpers;
using Newtonsoft.Json;
using SPT.Common.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Utils
{
    internal static class ConfigUtil
    {
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
    }
}
