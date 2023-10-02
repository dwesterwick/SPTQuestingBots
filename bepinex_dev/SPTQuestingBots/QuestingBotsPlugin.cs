using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.BotLogic;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.2.0")]
    [BepInDependency("xyz.drakia.bigbrain", "0.2.0")]
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.2.4")]
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
                LoggingController.LogInfo("Loading QuestingBots...enabling patches...");
                new Patches.MainMenuShowScreenPatch().Enable();
                new Patches.GameWorldOnDestroyPatch().Enable();
                new Patches.OnGameStartedPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();

                if (ConfigController.Config.InitialPMCSpawns.Enabled)
                {
                    new Patches.BossLocationSpawnActivatePatch().Enable();
                    new Patches.InitBossSpawnLocationPatch().Enable();
                    new Patches.BotOwnerCreatePatch().Enable();
                }

                LoggingController.LogInfo("Loading QuestingBots...enabling controllers...");
                this.GetOrAddComponent<LocationController>();
                this.GetOrAddComponent<BotQuestController>();
                this.GetOrAddComponent<BotGenerator>();

                List<string> botBrainsToChange = BotBrains.AllBots.ToList();
                LoggingController.LogInfo("Loading QuestingBots...changing bot brains: " + string.Join(", ", botBrainsToChange));
                BrainManager.AddCustomLayer(typeof(BotObjectiveLayer), botBrainsToChange, ConfigController.Config.BrainLayerPriority);

                if (ConfigController.Config.Debug.Enabled)
                {
                    new Patches.OnBeenKilledByAggressorPatch().Enable();
                }

                if (ConfigController.Config.Debug.ShowZoneOutlines)
                {
                    this.GetOrAddComponent<PathRender>();
                }

                if (ConfigController.Config.InitialPMCSpawns.Enabled)
                {
                    Logger.LogInfo("Initial PMC spawning is enabled. Adjusting PMC conversion chances...");
                    ConfigController.AdjustPMCConversionChances(ConfigController.Config.InitialPMCSpawns.ServerPMCConversionFactor);
                }

                QuestingBotsPluginConfig.BuildConfigOptions(Config);
            }

            Logger.LogInfo("Loading QuestingBots...done.");
        }
    }
}
