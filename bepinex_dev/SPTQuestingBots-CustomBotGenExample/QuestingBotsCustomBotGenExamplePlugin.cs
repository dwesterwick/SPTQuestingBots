using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots_CustomBotGenExample
{
    [BepInDependency("com.DanW.QuestingBots", "0.10.0")]
    [BepInPlugin("com.DanW.QuestingBotsCustomBotGenExample", "DanW-QuestingBots-CustomBotGenExample", "1.3.0")]
    public class QuestingBotsCustomBotGenExamplePlugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> Enabled;
        public static ConfigEntry<KeyboardShortcut> SpawnBotKey;
        public static ConfigEntry<int> MaxAliveBots;

        protected void Awake()
        {
            Logger.LogInfo("Loading QuestingBotsCustomBotGenExample...");

            LoggingController.Logger = Logger;

            enableParalysis();
            addConfigOptions();

            BotGenerator.RegisterBotGenerator<TestBotGenerator>();

            Logger.LogInfo("Loading QuestingBotsCustomBotGenExample...done.");
        }

        private void addConfigOptions()
        {
            Enabled = Config.Bind("Main", "Enabled", true, "Allow test bots to spawn");

            SpawnBotKey = Config.Bind("Main", "Key for Spawning Test Bots", new KeyboardShortcut(KeyCode.F5),
                $"A test bot will spawn {TestBotGenerator.DistanceToSpawnFromMainPlayer}m in front of you when you press this key");

            MaxAliveBots = Config.Bind("Main", "Max Alive Test Bots", 1,
                new ConfigDescription("Maximum number of test bots that are allowed to be alive at the same time", new AcceptableValueRange<int>(1, 5)));
        }

        private void enableParalysis()
        {
            IEnumerable<SPTQuestingBots.Models.BotBrainType> allBrains = BotBrainHelpers.GetAllBrains();
            BrainManager.AddCustomLayer(typeof(ParalyzeLayer), allBrains.ToStringList(), 999);
        }
    }
}
