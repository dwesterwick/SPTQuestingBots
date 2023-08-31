using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;

namespace SPTQuestingBots
{
    public static class QuestingBotsPluginConfig
    {
        public static ConfigEntry<bool> QuestingEnabled;

        public static void BuildConfigOptions(ConfigFile Config)
        {
            QuestingEnabled = Config.Bind("Main", "Enable Questing", true, "Allow bots to quest");
        }
    }
}
