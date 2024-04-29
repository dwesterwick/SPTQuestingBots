using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.4.3")]
    [BepInDependency("xyz.drakia.bigbrain", "0.4.0")]
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.5.0")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        private void Awake()
        {
            Patches.CheckSPTVersionPatch.MinVersion = "3.8.0.0";
            Patches.CheckSPTVersionPatch.MaxVersion = "3.8.99.0";

            Logger.LogInfo("Loading QuestingBots...");
            LoggingController.Logger = Logger;
            ModName = Info.Metadata.Name;

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            if (ConfigController.GetConfig() == null)
            {
                Chainloader.DependencyErrors.Add("Could not load " + ModName + " because it cannot communicate with the server. Please ensure the mod has been installed correctly.");
                return;
            }
            
            if (ConfigController.Config.Enabled)
            {
                LoggingController.LogInfo("Loading QuestingBots...enabling patches...");

                new Patches.CheckSPTVersionPatch().Enable();
                new Patches.AddActivePlayerPatch().Enable();
                new Patches.BotsControllerStopPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.AirdropLandPatch().Enable();
                new Patches.ServerRequestPatch().Enable();
                new Patches.CheckLookEnemyPatch().Enable();
                
                if (ConfigController.Config.BotSpawns.Enabled)
                {
                    new Patches.GameStartPatch().Enable();
                    new Patches.MatchmakerFinalCountdownUpdatePatch().Enable();
                    new Patches.ActivateBotsByWavePatch().Enable();
                    new Patches.ActivateBotsByWavePatch2().Enable();
                    new Patches.AddEnemyPatch().Enable();

                    if (ConfigController.Config.BotSpawns.SpawnInitialBossesFirst)
                    {
                        new Patches.InitBossSpawnLocationPatch().Enable();
                    }
                    
                    if (ConfigController.Config.BotSpawns.LimitInitialBossSpawns.Enabled)
                    {
                        new Patches.BossLocationSpawnActivatePatch().Enable();
                    }

                    if (ConfigController.Config.BotSpawns.AdvancedEFTBotCountManagement)
                    {
                        new Patches.GetListByZonePatch().Enable();
                        new Patches.ExceptAIPatch().Enable();
                        new Patches.BotDiedPatch().Enable();
                        new Patches.TryToSpawnInZoneAndDelayPatch().Enable();
                    }

                    Logger.LogInfo("Bot spawning is enabled. Adjusting PMC conversion chances...");
                    ConfigController.AdjustPMCConversionChances(0, false);
                }
                
                // Add options to the F12 menu
                QuestingBotsPluginConfig.BuildConfigOptions(Config);
                
                performLobotomies();
            }

            Logger.LogInfo("Loading QuestingBots...done.");
        }

        private void performLobotomies()
        {
            IEnumerable<BotBrainType> allNonSniperBrains = BotBrainHelpers.GetAllNonSniperBrains();
            IEnumerable<BotBrainType> allBrains = allNonSniperBrains.AddAllSniperBrains();

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for sleeping: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Sleep.SleepingLayer), allBrains.ToStringList(), 99);

            if (!ConfigController.Config.Questing.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for questing: " + string.Join(", ", allNonSniperBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Objective.BotObjectiveLayer), allNonSniperBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriority);

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for following: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerLayer), allBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriority + 1);
        }
    }
}
