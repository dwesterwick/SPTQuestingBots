using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using EFT;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;
using UnityEngine;

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
        GroundZero = 512,

        All = Customs | Factory | Interchange | Labs | Lighthouse | Reserve | Shoreline | Streets | Woods | GroundZero,
    }

    [Flags]
    public enum BotTypeException
    {
        SniperScavs = 1,
        Rogues = 2,
        Raiders = 4,
        BossesAndFollowers = 8,

        All = SniperScavs | Rogues | Raiders | BossesAndFollowers,
    }

    public static class QuestingBotsPluginConfig
    {
        public static Dictionary<string, TarkovMaps> TarkovMapIDToEnum = new Dictionary<string, TarkovMaps>();
        public static Dictionary<WildSpawnType, BotTypeException> ExceptionFlagForWildSpawnType = new Dictionary<WildSpawnType, BotTypeException>();

        public static ConfigEntry<bool> QuestingEnabled;
        public static ConfigEntry<bool> SprintingEnabled;
        public static ConfigEntry<int> MinSprintingDistance;

        public static ConfigEntry<bool> SleepingEnabled;
        public static ConfigEntry<bool> SleepingEnabledForQuestingBots;
        public static ConfigEntry<int> SleepingMinDistanceToYou;
        public static ConfigEntry<int> SleepingMinDistanceToPMCs;
        public static ConfigEntry<TarkovMaps> MapsToAllowSleepingForQuestingBots;
        public static ConfigEntry<BotTypeException> SleeplessBotTypes;

        public static ConfigEntry<bool> ShowBotInfoOverlays;
        public static ConfigEntry<bool> ShowBotPathOverlays;
        public static ConfigEntry<bool> ShowQuestInfoOverlays;
        public static ConfigEntry<bool> ShowQuestInfoForSpawnSearchQuests;
        public static ConfigEntry<int> QuestOverlayFontSize;
        public static ConfigEntry<int> QuestOverlayMaxDistance;

        public static ConfigEntry<bool> CreateQuestLocations;
        public static ConfigEntry<string> QuestLocationName;
        public static ConfigEntry<KeyboardShortcut> StoreQuestLocationKey;

        public static void BuildConfigOptions(ConfigFile Config)
        {
            indexMapIDs();
            indexWildSpawnTypeExceptions();

            QuestingEnabled = Config.Bind("Main", "Enable Questing",
                true, "Allow bots to quest");
            SprintingEnabled = Config.Bind("Main", "Allow Bots to Sprint while Questing",
                true, "Allow bots to sprint while questing. This does not affect their ability to sprint when they're not questing.");
            MinSprintingDistance = Config.Bind("Main", "Sprinting Distance Limit from Objectives (m)",
                3, new ConfigDescription("Bots will not be allowed to sprint if they are within this distance from their objective", new AcceptableValueRange<int>(0, 75)));

            SleepingEnabled = Config.Bind("AI Limiter", "Enable AI Limiting",
                false, "Improve FPS by minimizing CPU load for AI out of certain ranges");
            SleepingEnabledForQuestingBots = Config.Bind("AI Limiter", "Enable AI Limiting for Bots That Are Questing",
                true, "Allow AI to be disabled for bots that are questing");
            MapsToAllowSleepingForQuestingBots = Config.Bind("AI Limiter", "Maps to Allow AI Limiting for Bots That Are Questing",
                TarkovMaps.Streets, "Only allow AI to be disabled for bots that are questing on the selected maps");
            SleeplessBotTypes = Config.Bind("AI Limiter", "Bot Types that Cannot be Disabled",
                BotTypeException.SniperScavs | BotTypeException.Rogues, "These bot types will never be disabled by the AI limiter");
            SleepingMinDistanceToYou = Config.Bind("AI Limiter", "Distance from You (m)",
                200, new ConfigDescription("AI will only be disabled if it's more than this distance from you", new AcceptableValueRange<int>(50, 1000)));
            SleepingMinDistanceToPMCs = Config.Bind("AI Limiter", "Distance from PMCs (m)",
                75, new ConfigDescription("AI will only be disabled if it's more than this distance from other PMC's", new AcceptableValueRange<int>(25, 1000)));

            ShowBotInfoOverlays = Config.Bind("Debug", "Show Bot Info Overlays",
                false, "Show information about what each bot is doing");
            ShowBotPathOverlays = Config.Bind("Debug", "Show Bot Path Overlays",
                false, new ConfigDescription("Show the target position for each bot that is questing", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ShowQuestInfoOverlays = Config.Bind("Debug", "Show Quest Info Overlays",
                false, "Show information about every nearby quest objective location");
            ShowQuestInfoForSpawnSearchQuests = Config.Bind("Debug", "Show Quest Info for Spawn-Search Quests",
                false, new ConfigDescription("Include quest markers and information for spawn-search quests like 'Spawn Point Wander' and 'Boss Hunter' quests", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            QuestOverlayMaxDistance = Config.Bind("Debug", "Max Distance (m) to Show Quest Info",
                100, new ConfigDescription("Quest markers and info overlays will only be shown if the objective location is within this distance from you", new AcceptableValueRange<int>(10, 300)));
            QuestOverlayFontSize = Config.Bind("Debug", "Font Size for Quest Info",
                16, new ConfigDescription("Font Size for Quest Overlays", new AcceptableValueRange<int>(12, 36)));

            CreateQuestLocations = Config.Bind("Custom Quest Locations", "Enable Quest Location Saving",
                false, new ConfigDescription("Allow custom quest locations to be saved", null, new ConfigurationManagerAttributes { Order = 3, IsAdvanced = true }));
            QuestLocationName = Config.Bind("Custom Quest Locations", "Quest Location Name",
                "Custom Quest Location", new ConfigDescription("Name of the next quest location that will be stored", null, new ConfigurationManagerAttributes { Order = 2, IsAdvanced = true }));
            StoreQuestLocationKey = Config.Bind("Custom Quest Locations", "Store New Quest Location",
                new KeyboardShortcut(KeyCode.KeypadEnter), new ConfigDescription("Store your current location as a quest location", null, new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }));
        }

        private static void indexMapIDs()
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
            TarkovMapIDToEnum.Add("Sandbox", TarkovMaps.GroundZero);
        }

        private static void indexWildSpawnTypeExceptions()
        {
            IEnumerable<BotBrainType> sniperScavBrains = Enumerable.Empty<BotBrainType>().AddSniperScavBrain();
            IEnumerable<BotBrainType> rogueBrains = Enumerable.Empty<BotBrainType>().AddRogueBrain();
            IEnumerable<BotBrainType> raiderBrains = Enumerable.Empty<BotBrainType>().AddRaiderBrain();
            IEnumerable<BotBrainType> bossAndFollowerBrains = Enumerable.Empty<BotBrainType>().AddAllNormalBossAndFollowerBrains();

            addBrainsToExceptions(sniperScavBrains, BotTypeException.SniperScavs);
            addBrainsToExceptions(rogueBrains, BotTypeException.Rogues);
            addBrainsToExceptions(raiderBrains, BotTypeException.Raiders);
            addBrainsToExceptions(bossAndFollowerBrains, BotTypeException.BossesAndFollowers);
        }

        private static void addBrainsToExceptions(IEnumerable<BotBrainType> brainTypes, BotTypeException botTypeException)
        {
            foreach (BotBrainType brainType in brainTypes)
            {
                if (!ExceptionFlagForWildSpawnType.ContainsKey(brainType.SpawnType))
                {
                    ExceptionFlagForWildSpawnType.Add(brainType.SpawnType, botTypeException);
                }
                else
                {
                    ExceptionFlagForWildSpawnType[brainType.SpawnType] |= botTypeException;
                }
            }
        }
    }
}
