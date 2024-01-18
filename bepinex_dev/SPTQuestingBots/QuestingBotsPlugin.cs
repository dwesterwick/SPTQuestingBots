using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots;
using SPTQuestingBots.Models;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.3.3")]
    [BepInDependency("xyz.drakia.bigbrain", "0.3.1")]
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.4.0")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        private void Awake()
        {
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
                LoggingController.LogInfo("Loading QuestingBots...enabling patches and controllers...");

                new Patches.CheckSPTVersionPatch().Enable();
                new Patches.GameWorldCreatePatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.AirdropLandPatch().Enable();

                if (ConfigController.Config.InitialPMCSpawns.Enabled)
                {
                    new Patches.BossLocationSpawnActivatePatch().Enable();
                    new Patches.InitBossSpawnLocationPatch().Enable();
                    new Patches.BotOwnerCreatePatch().Enable();
                    new Patches.ReadyToPlayPatch().Enable();
                    
                    Logger.LogInfo("Initial PMC spawning is enabled. Adjusting PMC conversion chances...");
                    ConfigController.AdjustPMCConversionChances(0, false);
                }

                if (ConfigController.Config.AdjustPScavChance.Enabled)
                {
                    new Patches.LoadBotsPatch().Enable();
                }

                this.GetOrAddComponent<LocationController>();

                if (ConfigController.Config.Questing.Enabled)
                {
                    this.GetOrAddComponent<BotQuestBuilder>();
                }

                if (ConfigController.Config.Debug.Enabled)
                {
                    if (ConfigController.Config.Debug.ShowZoneOutlines || ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        this.GetOrAddComponent<PathRender>();
                    }
                }

                // Add options to the F12 menu
                QuestingBotsPluginConfig.BuildConfigOptions(Config);
                this.GetOrAddComponent<DebugController>();

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
