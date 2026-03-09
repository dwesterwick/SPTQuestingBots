using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using QuestingBots.Components;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.8.0")]
    [BepInDependency("xyz.drakia.bigbrain", "1.4.0")]
    [BepInPlugin(ModInfo.GUID, ModInfo.MODNAME, ModInfo.MOD_VERSION)]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        protected void Awake()
        {
            Patches.TarkovInitPatch.MinVersion = "4.0.0.0";
            Patches.TarkovInitPatch.MaxVersion = "4.0.99.0";

            Logger.LogInfo("Loading QuestingBots...");
            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));
            if (!confirmNoPreviousVersionExists())
            {
                Chainloader.DependencyErrors.Add("An older version of " + ModInfo.MODNAME + " still exists in '/BepInEx/plugins'. Please remove QuestingBots.dll from that directory, or this mod will not work correctly.");
                return;
            }

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            if (ConfigController.GetConfig() == null)
            {
                Chainloader.DependencyErrors.Add("Could not load " + ModInfo.MODNAME + " because it cannot communicate with the server. Please ensure the mod has been installed correctly.");
                return;
            }

            new Patches.MenuShowPatch().Enable();

            if (ConfigController.Config.Enabled)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...enabling patches...");

                new Patches.TarkovInitPatch().Enable();
                new Patches.BotsControllerSetSettingsPatch().Enable();
                new Patches.BotsControllerStopPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.AirdropLandPatch().Enable();
                new Patches.ServerRequestPatch().Enable();
                new Patches.CheckLookEnemyPatch().Enable();
                new Patches.ReturnToPoolPatch().Enable();
                new Patches.BotOwnerSprintPatch().Enable();

                new Patches.Lighthouse.MineDirectionalShouldExplodePatch().Enable();
                new Patches.Lighthouse.LighthouseTraderZoneAwakePatch().Enable();
                new Patches.Lighthouse.LighthouseTraderZonePlayerAttackPatch().Enable();

                if (ConfigController.Config.Debug.Enabled)
                {
                    new Patches.Debug.ProcessSourceOcclusionPatch().Enable();
                    //new Patches.Debug.HandleFinishedTaskPatch().Enable();
                    //new Patches.Debug.HandleFinishedTaskPatch2().Enable();
                }
                
                if (ConfigController.Config.BotSpawns.Enabled)
                {
                    new Patches.Spawning.GameStartPatch().Enable();
                    new Patches.Spawning.TimeHasComeScreenClassChangeStatusPatch().Enable();
                    new Patches.Spawning.ActivateBossesByWavePatch().Enable();
                    new Patches.Spawning.AddEnemyPatch().Enable();
                    new Patches.Spawning.TryLoadBotsProfilesOnStartPatch().Enable();
                    new Patches.Spawning.SetNewBossPatch().Enable();
                    new Patches.Spawning.GetAllBossPlayersPatch().Enable();

                    new Patches.Spawning.Advanced.GetListByZonePatch().Enable();
                    new Patches.Spawning.Advanced.ExceptAIPatch().Enable();
                    new Patches.Spawning.Advanced.BotDiedPatch().Enable();
                    new Patches.Spawning.Advanced.TryToSpawnInZoneAndDelayPatch().Enable();

                    new Patches.Spawning.ScavLimits.SpawnPointIsValidPatch().Enable();
                    new Patches.Spawning.ScavLimits.TrySpawnFreeAndDelayPatch().Enable();
                    new Patches.Spawning.ScavLimits.NonWavesSpawnScenarioCreatePatch().Enable();
                    new Patches.Spawning.ScavLimits.BotsControllerStopPatch().Enable();

                    if (ConfigController.Config.BotSpawns.SpawnInitialBossesFirst)
                    {
                        new Patches.Spawning.InitBossSpawnLocationPatch().Enable();
                    }

                    if (ConfigController.Config.BotSpawns.PMCHostilityAdjustments.Enabled && ConfigController.Config.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
                    {
                        new Patches.Spawning.BotsGroupIsPlayerEnemyPatch().Enable();
                    }

                    if (ConfigController.Config.BotSpawns.PMCs.Enabled)
                    {
                        BotGenerator.RegisterBotGenerator<Components.Spawning.PMCGenerator>();
                        Singleton<LoggingUtil>.Instance.LogInfo("Enabled PMC bot generation");
                    }
                    if (ConfigController.Config.BotSpawns.PScavs.Enabled)
                    {
                        BotGenerator.RegisterBotGenerator<Components.Spawning.PScavGenerator>(true);
                        Singleton<LoggingUtil>.Instance.LogInfo("Enabled PScav bot generation");
                    }
                }

                if ((ConfigController.Config.BotSpawns.Enabled && ConfigController.Config.BotSpawns.PScavs.Enabled) || ConfigController.Config.AdjustPScavChance.Enabled)
                {
                    new Patches.PScavProfilePatch().Enable();
                }
                
                // Add options to the F12 menu
                QuestingBotsPluginConfig.BuildConfigOptions(Config);
                
                this.GetOrAddComponent<TarkovData>();
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...done.");
        }

        private bool confirmNoPreviousVersionExists()
        {
            string oldPath = AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/QuestingBots.dll";
            if (File.Exists(oldPath))
            {
                return false;
            }

            return true;
        }
    }
}
