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

        public static ConfigEntry<bool> SleepingEnabled;
        public static ConfigEntry<bool> SleepingEnabledForQuestingBots;
        public static ConfigEntry<int> SleepingMinDistanceToYou;
        public static ConfigEntry<int> SleepingMinDistanceToPMCs;

        public static void BuildConfigOptions(ConfigFile Config)
        {
            QuestingEnabled = Config.Bind("Main", "Enable Questing", true, "Allow bots to quest");

            SleepingEnabled = Config.Bind("AI Limiter", "Enable AI Limiting", false, "Improve FPS by minimizing CPU load for AI out of certain ranges");
            SleepingEnabledForQuestingBots = Config.Bind("AI Limiter", "Enable AI Limiting for Bots That Are Questing",
                false, "Allow AI to be disabled for bots that are questing");
            SleepingMinDistanceToYou = Config.Bind("AI Limiter", "Distance from You", 400,
                new ConfigDescription("AI will only be disabled if it's more than this distance from you", new AcceptableValueRange<int>(50, 2000)));
            SleepingMinDistanceToPMCs = Config.Bind("AI Limiter", "Distance from PMCs", 75,
                new ConfigDescription("AI will only be disabled if it's more than this distance from other PMC's", new AcceptableValueRange<int>(50, 2000)));
        }
    }
}
