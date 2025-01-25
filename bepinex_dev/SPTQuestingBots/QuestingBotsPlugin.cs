using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Bootstrap;
using DrakiaXYZ.BigBrain.Brains;
using SPTQuestingBots.Components;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using SPTQuestingBots.Models;

namespace SPTQuestingBots
{
    [BepInIncompatibility("com.pandahhcorp.aidisabler")]
    [BepInIncompatibility("com.dvize.AILimit")]
    [BepInDependency("xyz.drakia.waypoints", "1.6.2")]
    [BepInDependency("xyz.drakia.bigbrain", "1.2.0")]
    [BepInPlugin("com.DanW.QuestingBots", "DanW-QuestingBots", "0.9.0")]
    public class QuestingBotsPlugin : BaseUnityPlugin
    {
        public static string ModName { get; private set; } = "???";

        protected void Awake()
        {
            Patches.TarkovInitPatch.MinVersion = "3.10.0.0";
            Patches.TarkovInitPatch.MaxVersion = "3.10.99.0";

            Logger.LogInfo("Loading QuestingBots...");
            LoggingController.Logger = Logger;
            ModName = Info.Metadata.Name;

            if (!confirmNoPreviousVersionExists())
            {
                Chainloader.DependencyErrors.Add("An older version of " + ModName + " still exists in '/BepInEx/plugins'. Please remove SPTQuestingBots.dll from that directory, or this mod will not work correctly.");
                return;
            }

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            if (ConfigController.GetConfig() == null)
            {
                Chainloader.DependencyErrors.Add("Could not load " + ModName + " because it cannot communicate with the server. Please ensure the mod has been installed correctly.");
                return;
            }

            new Patches.MenuShowPatch().Enable();

            if (ConfigController.Config.Enabled)
            {
                LoggingController.LogInfo("Loading QuestingBots...enabling patches...");

                new Patches.TarkovInitPatch().Enable();
                new Patches.AddActivePlayerPatch().Enable();
                new Patches.BotsControllerStopPatch().Enable();
                new Patches.BotOwnerBrainActivatePatch().Enable();
                new Patches.IsFollowerSuitableForBossPatch().Enable();
                new Patches.OnBeenKilledByAggressorPatch().Enable();
                new Patches.AirdropLandPatch().Enable();
                new Patches.ServerRequestPatch().Enable();
                new Patches.CheckLookEnemyPatch().Enable();
                new Patches.MineDirectionalShouldExplodePatch().Enable();
                new Patches.LighthouseTraderZoneAwakePatch().Enable();
                new Patches.LighthouseTraderZonePlayerAttackPatch().Enable();
                //new Patches.IsEnemyByChancePatch().Enable();
                //new Patches.IsPlayerEnemyPatch().Enable();

                if (ConfigController.Config.Debug.Enabled)
                {
                    new Patches.ProcessSourceOcclusionPatch().Enable();
                }
                
                if (ConfigController.Config.BotSpawns.Enabled)
                {
                    new Patches.GameStartPatch().Enable();
                    new Patches.TimeHasComeScreenClassChangeStatusPatch().Enable();
                    new Patches.ActivateBossesByWavePatch().Enable();
                    new Patches.AddEnemyPatch().Enable();
                    new Patches.TryLoadBotsProfilesOnStartPatch().Enable();
                    new Patches.SetNewBossPatch().Enable();

                    if (ConfigController.Config.BotSpawns.SpawnInitialBossesFirst)
                    {
                        new Patches.InitBossSpawnLocationPatch().Enable();
                    }
                    
                    if (ConfigController.Config.BotSpawns.AdvancedEFTBotCountManagement.Enabled)
                    {
                        new Patches.GetListByZonePatch().Enable();
                        new Patches.ExceptAIPatch().Enable();
                        new Patches.BotDiedPatch().Enable();
                        new Patches.TryToSpawnInZoneAndDelayPatch().Enable();
                    }

                    if (ConfigController.Config.BotSpawns.PMCs.Enabled)
                    {
                        BotGenerator.RegisterBotGenerator<Components.Spawning.PMCGenerator>();
                        Logger.LogInfo("Enabled PMC bot generation");
                    }
                    if (ConfigController.Config.BotSpawns.PScavs.Enabled)
                    {
                        BotGenerator.RegisterBotGenerator<Components.Spawning.PScavGenerator>(true);
                        Logger.LogInfo("Enabled PScav bot generation");
                    }
                }
                
                // Add options to the F12 menu
                QuestingBotsPluginConfig.BuildConfigOptions(Config);
                
                performLobotomies();

                this.GetOrAddComponent<TarkovData>();
            }

            Logger.LogInfo("Loading QuestingBots...done.");
        }

        private void performLobotomies()
        {
            IEnumerable<BotBrainType> allNonSniperBrains = BotBrainHelpers.GetAllNonSniperBrains();
            IEnumerable<BotBrainType> allBrains = allNonSniperBrains.AddAllSniperBrains();

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for sleeping: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Sleep.SleepingLayer), allBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriorities.Sleeping);

            if (!ConfigController.Config.Questing.Enabled)
            {
                return;
            }

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for questing: " + string.Join(", ", allNonSniperBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Objective.BotObjectiveLayer), allNonSniperBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriorities.Questing);

            LoggingController.LogInfo("Loading QuestingBots...changing bot brains for following: " + string.Join(", ", allBrains));
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerLayer), allBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriorities.Following);
            BrainManager.AddCustomLayer(typeof(BotLogic.Follow.BotFollowerRegroupLayer), allBrains.ToStringList(), ConfigController.Config.Questing.BrainLayerPriorities.Regrouping);
        }

        private bool confirmNoPreviousVersionExists()
        {
            string oldPath = AppDomain.CurrentDomain.BaseDirectory + "/BepInEx/plugins/SPTQuestingBots.dll";
            if (File.Exists(oldPath))
            {
                return false;
            }

            return true;
        }
    }
}
