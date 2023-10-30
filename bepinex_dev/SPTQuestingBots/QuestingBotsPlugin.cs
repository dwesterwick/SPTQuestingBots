using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.BotLogic;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.3.2")]
    [BepInDependency("xyz.drakia.bigbrain", "0.3.1")]
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.2.10")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Loading QuestingBots...");

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            ConfigController.GetConfig();
            LoggingController.Logger = Logger;

            if (ConfigController.Config.Enabled)
            {
                LoggingController.LogInfo("Loading QuestingBots...enabling patches and controllers...");

                new Patches.MainMenuShowScreenPatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();

                if (ConfigController.Config.InitialPMCSpawns.Enabled)
                {
                    new Patches.BossLocationSpawnActivatePatch().Enable();
                    new Patches.InitBossSpawnLocationPatch().Enable();
                    new Patches.BotOwnerCreatePatch().Enable();

                    Logger.LogInfo("Initial PMC spawning is enabled. Adjusting PMC conversion chances...");
                    ConfigController.AdjustPMCConversionChances(ConfigController.Config.InitialPMCSpawns.ServerPMCConversionFactor);
                }

                this.GetOrAddComponent<LocationController>();
                this.GetOrAddComponent<BotQuestController>();
                this.GetOrAddComponent<BotGenerator>();

                if (ConfigController.Config.Debug.Enabled)
                {
                    // This patch just writes a debug message saying which bot was killed and by whom
                    new Patches.OnBeenKilledByAggressorPatch().Enable();

                    if (ConfigController.Config.Debug.ShowZoneOutlines || ConfigController.Config.Debug.ShowFailedPaths)
                    {
                        this.GetOrAddComponent<PathRender>();
                    }
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

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for questing: " + string.Join(", ", allNonSniperBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Objective.BotObjectiveLayer), allNonSniperBrains.ToStringList(), ConfigController.Config.BrainLayerPriority);

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for following: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerLayer), allBrains.ToStringList(), ConfigController.Config.BrainLayerPriority + 1);

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for sleeping: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Sleep.SleepingLayer), allBrains.ToStringList(), 99);
        }
    }
}
