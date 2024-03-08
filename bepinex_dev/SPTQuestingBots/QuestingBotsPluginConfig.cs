using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using Comfort.Common;

namespace SPTQuestingBots
{
    [Flags]
    public enum TarkovMaps
    {
        Customs = 1,
        Factory = 2,
        Interchange = 4,
        Labs = 8,
        Lighthouse = 16,
        Reserve = 32,
        Shoreline = 64,
        Streets = 128,
        Woods = 256,

        All = Customs | Factory | Interchange | Labs | Lighthouse | Reserve | Shoreline | Streets | Woods,
    }

    public static class QuestingBotsPluginConfig
    {
        public static Dictionary<string, TarkovMaps> TarkovMapIDToEnum = new Dictionary<string, TarkovMaps>();

        public static ConfigEntry<bool> QuestingEnabled;
        public static ConfigEntry<bool> SprintingEnabled;

        public static ConfigEntry<bool> SleepingEnabled;
        public static ConfigEntry<bool> SleepingEnabledForQuestingBots;
        public static ConfigEntry<int> SleepingMinDistanceToYou;
        public static ConfigEntry<int> SleepingMinDistanceToPMCs;
        public static ConfigEntry<TarkovMaps> MapsToAllowSleepingForQuestingBots;

        public static ConfigEntry<bool> ShowBotInfoOverlays;
        public static ConfigEntry<bool> ShowQuestInfoOverlays;
        public static ConfigEntry<bool> ShowQuestInfoForSpawnSearchQuests;
        public static ConfigEntry<int> QuestOverlayFontSize;
        public static ConfigEntry<int> QuestOverlayMaxDistance;

        public static void BuildConfigOptions(ConfigFile Config)
        {
            TarkovMapIDToEnum.Add("bigmap", TarkovMaps.Customs);
            TarkovMapIDToEnum.Add("factory4_day", TarkovMaps.Factory);
            TarkovMapIDToEnum.Add("factory4_night", TarkovMaps.Factory);
            TarkovMapIDToEnum.Add("Interchange", TarkovMaps.Interchange);
            TarkovMapIDToEnum.Add("laboratory", TarkovMaps.Labs);
            TarkovMapIDToEnum.Add("Lighthouse", TarkovMaps.Lighthouse);
            TarkovMapIDToEnum.Add("RezervBase", TarkovMaps.Reserve);
            TarkovMapIDToEnum.Add("Shoreline", TarkovMaps.Shoreline);
            TarkovMapIDToEnum.Add("TarkovStreets", TarkovMaps.Streets);
            TarkovMapIDToEnum.Add("Woods", TarkovMaps.Woods);

            QuestingEnabled = Config.Bind("Main", "Enable Questing",
                true, "Allow bots to quest");
            SprintingEnabled = Config.Bind("Main", "Allow Bots to Sprint while Questing",
                true, "Allow bots to sprint while questing. This does not affect their ability to sprint when they're not questing.");

            SleepingEnabled = Config.Bind("AI Limiter", "Enable AI Limiting",
                false, "Improve FPS by minimizing CPU load for AI out of certain ranges");
            SleepingEnabledForQuestingBots = Config.Bind("AI Limiter", "Enable AI Limiting for Bots That Are Questing",
                true, "Allow AI to be disabled for bots that are questing");
            MapsToAllowSleepingForQuestingBots = Config.Bind("AI Limiter", "Maps to Allow AI Limiting for Bots That Are Questing",
                TarkovMaps.Streets, "Only allow AI to be disabled for bots that are questing on the selected maps");
            SleepingMinDistanceToYou = Config.Bind("AI Limiter", "Distance from You (m)",
                200, new ConfigDescription("AI will only be disabled if it's more than this distance from you", new AcceptableValueRange<int>(50, 1000)));
            SleepingMinDistanceToPMCs = Config.Bind("AI Limiter", "Distance from PMCs (m)",
                75, new ConfigDescription("AI will only be disabled if it's more than this distance from other PMC's", new AcceptableValueRange<int>(25, 1000)));

            ShowBotInfoOverlays = Config.Bind("Debug", "Show Bot Info Overlays",
                false, "Show information about what each bot is doing");
            ShowQuestInfoOverlays = Config.Bind("Debug", "Show Quest Info Overlays",
                false, "Show information about every nearby quest objective location");
            ShowQuestInfoForSpawnSearchQuests = Config.Bind("Debug", "Show Quest Info for Spawn-Search Quests",
                false, "Include quest markers and information for spawn-search quests like 'Spawn Point Wander' and 'Boss Hunter' quests");
            QuestOverlayMaxDistance = Config.Bind("Debug", "Max Distance (m) to Show Quest Info",
                100, new ConfigDescription("Quest markers and info overlays will only be shown if the objective location is within this distance from you", new AcceptableValueRange<int>(10, 300)));
            QuestOverlayFontSize = Config.Bind("Debug", "Font Size for Quest Info",
                16, new ConfigDescription("Font Size for Quest Overlays", new AcceptableValueRange<int>(12, 36)));
        }
    }
}
