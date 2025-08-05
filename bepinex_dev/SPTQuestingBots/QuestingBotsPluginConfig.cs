using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Configuration;
using EFT;
using SPTQuestingBots.Controllers;
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

    [Flags]
    public enum QuestingBotType
    {
        QuestingLeaders = 1,
        QuestingFollowers = 2,
        NonQuestingBots = 4,
        AllQuestingBots = QuestingLeaders | QuestingFollowers,
        All = QuestingLeaders | QuestingFollowers | NonQuestingBots,
    }

    [Flags]
    public enum BotPathOverlayType
    {
        QuestTarget = 1,
        EFTTarget = 2,
        EFTCurrentCorner = 4,
        AllEFT = EFTTarget | EFTCurrentCorner,
        All = QuestTarget | EFTTarget | EFTCurrentCorner,
    }

    public static class QuestingBotsPluginConfig
    {
        public static Dictionary<string, TarkovMaps> TarkovMapIDToEnum = new Dictionary<string, TarkovMaps>();
        public static Dictionary<WildSpawnType, BotTypeException> ExceptionFlagForWildSpawnType = new Dictionary<WildSpawnType, BotTypeException>();

        public static ConfigEntry<bool> QuestingEnabled;
        public static ConfigEntry<bool> ShowSpawnDebugMessages;
        public static ConfigEntry<bool> SprintingEnabled;
        public static ConfigEntry<int> MinSprintingDistance;

        public static ConfigEntry<bool> ScavLimitsEnabled;
        public static ConfigEntry<float> ScavSpawningExclusionRadiusMapFraction;
        public static ConfigEntry<float> ScavSpawnRateLimit;
        public static ConfigEntry<int> ScavSpawnLimitThreshold;
        public static ConfigEntry<int> ScavMaxAliveLimit;

        public static ConfigEntry<bool> SleepingEnabled;
        public static ConfigEntry<bool> SleepingEnabledForQuestingBots;
        public static ConfigEntry<int> SleepingMinDistanceToHumansGlobal;
        public static ConfigEntry<int> SleepingMinDistanceToHumansCustoms;
        public static ConfigEntry<int> SleepingMinDistanceToHumansFactory;
        public static ConfigEntry<int> SleepingMinDistanceToHumansInterchange;
        public static ConfigEntry<int> SleepingMinDistanceToHumansLabs;
        public static ConfigEntry<int> SleepingMinDistanceToHumansLighthouse;
        public static ConfigEntry<int> SleepingMinDistanceToHumansReserve;
        public static ConfigEntry<int> SleepingMinDistanceToHumansShoreline;
        public static ConfigEntry<int> SleepingMinDistanceToHumansStreets;
        public static ConfigEntry<int> SleepingMinDistanceToHumansWoods;
        public static ConfigEntry<int> SleepingMinDistanceToHumansGroundZero;
        public static ConfigEntry<int> SleepingMinDistanceToQuestingBots;
        public static ConfigEntry<TarkovMaps> MapsToAllowSleepingForQuestingBots;
        public static ConfigEntry<BotTypeException> SleeplessBotTypes;
        public static ConfigEntry<int> MinBotsToEnableSleeping;

        public static ConfigEntry<QuestingBotType> ShowBotInfoOverlays;
        public static ConfigEntry<QuestingBotType> ShowBotPathOverlays;
        public static ConfigEntry<QuestingBotType> ShowBotPathVisualizations;
        public static ConfigEntry<BotPathOverlayType> BotPathOverlayTypes;
        public static ConfigEntry<bool> ShowQuestInfoOverlays;
        public static ConfigEntry<bool> ShowQuestInfoForSpawnSearchQuests;
        public static ConfigEntry<int> QuestOverlayFontSize;
        public static ConfigEntry<int> QuestOverlayMaxDistance;
        public static ConfigEntry<string> BotFilter;

        public static ConfigEntry<bool> CreateQuestLocations;
        public static ConfigEntry<bool> ShowCurrentLocation;
        public static ConfigEntry<string> QuestLocationName;
        public static ConfigEntry<KeyboardShortcut> StoreQuestLocationKey;

        public static void BuildConfigOptions(ConfigFile Config)
        {
            indexMapIDs();
            indexWildSpawnTypeExceptions();

            QuestingEnabled = Config.Bind("Main", "Enable Questing",
                true, "Allow bots to quest");
            ShowSpawnDebugMessages = Config.Bind("Main", "Show Debug Messages for Spawning",
                false, new ConfigDescription("Show additional debug messages to troubleshoot spawning issues", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            SprintingEnabled = Config.Bind("Main", "Allow Bots to Sprint while Questing",
                true, "Allow bots to sprint while questing. This does not affect their ability to sprint when they're not questing.");
            MinSprintingDistance = Config.Bind("Main", "Sprinting Distance Limit from Objectives (m)",
                3, new ConfigDescription("Bots will not be allowed to sprint if they are within this distance from their objective", new AcceptableValueRange<int>(0, 75)));

            if (ConfigController.Config.BotSpawns.Enabled)
            {
                ScavLimitsEnabled = Config.Bind("Scav Spawn Restrictions", "Enable Scav Spawn Restrictions",
                    true, "Restrict where and how frequently Scavs are allowed to spawn");
                ScavSpawningExclusionRadiusMapFraction = Config.Bind("Scav Spawn Restrictions", "Map Fraction for Scav Spawning Exclusion Radius",
                    0.1f, new ConfigDescription("Adjusts the distance (relative to the map size) that Scavs are allowed to spawn near human players, PMC's, and player Scavs", new AcceptableValueRange<float>(0.01f, 0.15f)));
                ScavSpawnRateLimit = Config.Bind("Scav Spawn Restrictions", "Permitted Scav Spawn Rate",
                    2.5f, new ConfigDescription("After the Scav spawn threshold is exceeded, only this number of Scavs will be allowed to spawn per minute (on average)", new AcceptableValueRange<float>(0.5f, 6f)));
                ScavSpawnLimitThreshold = Config.Bind("Scav Spawn Restrictions", "Threshold for Scav Spawn Rate Limit",
                    10, new ConfigDescription("The Scav spawn rate limit will only be active after this many Scavs spawn in the raid", new AcceptableValueRange<int>(1, 50)));
                ScavMaxAliveLimit = Config.Bind("Scav Spawn Restrictions", "Max Alive Scavs",
                    15, new ConfigDescription("The maximum number of Scavs that can be alive at the same time (including Sniper Scavs)", new AcceptableValueRange<int>(5, 25)));
            }

            int minDistanceAILimitNormal = ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.AllowZeroDistanceSleeping ? 0 : 50;
            int minDistanceAILimitQuesting = ConfigController.Config.Debug.Enabled && ConfigController.Config.Debug.AllowZeroDistanceSleeping ? 0 : 25;

            SleepingEnabled = Config.Bind("AI Limiter", "Enable AI Limiting",
                false, "Improve FPS by minimizing CPU load for AI out of certain ranges");
            SleepingEnabledForQuestingBots = Config.Bind("AI Limiter", "Enable AI Limiting for Bots That Are Questing",
                true, "Allow AI to be disabled for bots that are questing");
            MapsToAllowSleepingForQuestingBots = Config.Bind("AI Limiter", "Maps to Allow AI Limiting for Bots That Are Questing",
                TarkovMaps.Streets, "Only allow AI to be disabled for bots that are questing on the selected maps");
            SleeplessBotTypes = Config.Bind("AI Limiter", "Bot Types that Cannot be Disabled",
                BotTypeException.SniperScavs | BotTypeException.Rogues, "These bot types will never be disabled by the AI limiter");
            MinBotsToEnableSleeping = Config.Bind("AI Limiter", "Min Bots to Enable AI Limiting",
                15, new ConfigDescription("AI will only be disabled if there are at least this number of bots on the map", new AcceptableValueRange<int>(1, 30)));
            SleepingMinDistanceToQuestingBots = Config.Bind("AI Limiter", "Distance from Bots That Are Questing (m)",
                75, new ConfigDescription("AI will only be disabled if it's more than this distance from other questing bots (typically PMC's and player Scavs)", new AcceptableValueRange<int>(minDistanceAILimitQuesting, 1000)));

            SleepingMinDistanceToHumansGlobal = Config.Bind("AI Limiter", "Distance from Human Players (m)",
                200, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player. This takes priority over the map-specific advanced settings.", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000)));
            SleepingMinDistanceToHumansCustoms = Config.Bind("AI Limiter", "Distance from Human Players on Customs (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Customs", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansFactory = Config.Bind("AI Limiter", "Distance from Human Players on Factory (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Factory", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansInterchange = Config.Bind("AI Limiter", "Distance from Human Players on Interchange (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Interchange", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansLabs = Config.Bind("AI Limiter", "Distance from Human Players on Labs (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Labs", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansLighthouse = Config.Bind("AI Limiter", "Distance from Human Players on Lighthouse (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Lighthouse", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansReserve = Config.Bind("AI Limiter", "Distance from Human Players on Reserve (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Reserve", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansShoreline = Config.Bind("AI Limiter", "Distance from Human Players on Shoreline (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Shoreline", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansStreets = Config.Bind("AI Limiter", "Distance from Human Players on Streets (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Streets", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansWoods = Config.Bind("AI Limiter", "Distance from Human Players on Woods (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on Woods", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));
            SleepingMinDistanceToHumansGroundZero = Config.Bind("AI Limiter", "Distance from Human Players on GroundZero (m)",
                1000, new ConfigDescription("AI will only be disabled if it's more than this distance from a human player on GroundZero", new AcceptableValueRange<int>(minDistanceAILimitNormal, 1000), new ConfigurationManagerAttributes { IsAdvanced = true }));

            ShowBotInfoOverlays = Config.Bind("Debug", "Show Bot Info Overlays",
                (QuestingBotType)0, "Show information about what each bot is doing");
            ShowBotPathOverlays = Config.Bind("Debug", "Show Bot Path Overlays",
                (QuestingBotType)0, new ConfigDescription("Create markers for Bot Path Overlay Types that bots of each selected type are following", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ShowBotPathVisualizations = Config.Bind("Debug", "Show Bot Path Visualizations",
                (QuestingBotType)0, new ConfigDescription("Draw the path that bots of each selected type are following", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            BotPathOverlayTypes = Config.Bind("Debug", "Bot Path Overlay Types",
                BotPathOverlayType.QuestTarget, new ConfigDescription("The types of positions that will be shown for each bot that has path overlays enabled", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            ShowQuestInfoOverlays = Config.Bind("Debug", "Show Quest Info Overlays",
                false, "Show information about every nearby quest objective location");
            ShowQuestInfoForSpawnSearchQuests = Config.Bind("Debug", "Show Quest Info for Spawn-Search Quests",
                false, new ConfigDescription("Include quest markers and information for spawn-search quests like 'Spawn Point Wander' and 'Boss Hunter' quests", null, new ConfigurationManagerAttributes { IsAdvanced = true }));
            QuestOverlayMaxDistance = Config.Bind("Debug", "Max Distance (m) to Show Quest Info",
                100, new ConfigDescription("Quest markers and info overlays will only be shown if the objective location is within this distance from you", new AcceptableValueRange<int>(10, 300)));
            QuestOverlayFontSize = Config.Bind("Debug", "Font Size for Quest Info",
                16, new ConfigDescription("Font Size for Quest Overlays", new AcceptableValueRange<int>(12, 36))); 
            BotFilter = Config.Bind("Debug", "Bot Filter",
                "", new ConfigDescription("Show debug info only for bots listed e.g 2,4", null, new ConfigurationManagerAttributes { IsAdvanced = true }));

            CreateQuestLocations = Config.Bind("Custom Quest Locations", "Enable Quest Location Saving",
                false, new ConfigDescription("Allow custom quest locations to be saved", null, new ConfigurationManagerAttributes { Order = 4, IsAdvanced = true }));
            ShowCurrentLocation = Config.Bind("Custom Quest Locations", "Display Current Location",
                false, new ConfigDescription("Display your current (x,y,z) coordinates on the screen", null, new ConfigurationManagerAttributes { Order = 3, IsAdvanced = true }));
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
            TarkovMapIDToEnum.Add("Sandbox_high", TarkovMaps.GroundZero);
        }

        private static void indexWildSpawnTypeExceptions()
        {
            IEnumerable<BotBrainType> sniperScavBrains = Enumerable.Empty<BotBrainType>().AddSniperScavBrain();
            IEnumerable<BotBrainType> rogueBrains = Enumerable.Empty<BotBrainType>().AddRogueBrain();
            IEnumerable<BotBrainType> raiderBrains = Enumerable.Empty<BotBrainType>().AddRaiderBrain();

            IEnumerable<BotBrainType> bossAndFollowerBrains = Enumerable.Empty<BotBrainType>()
                .AddAllNormalBossAndFollowerBrains()
                .AddZryachiyAndFollowerBrains();

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
