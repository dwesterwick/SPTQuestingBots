using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots
{
    [BepInPlugin(ModInfo.GUID, ModInfo.MODNAME, ModInfo.MOD_VERSION)]
    internal class QuestingBotsPlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> GodMode = null!;
        public static ConfigEntry<bool> InfiniteStamina = null!;
        public static ConfigEntry<bool> InfiniteHydrationAndEnergy = null!;

        protected void Awake()
        {
            Logger.LogInfo("Loading QuestingBots...");

            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            if (ConfigUtil.CurrentConfig.Enabled)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...enabled");

            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...done.");
        }
    }
}
