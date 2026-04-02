using BepInEx;
using BepInEx.Bootstrap;
using Comfort.Common;
using QuestingBots.Components.Spawning;
using QuestingBots.Helpers;
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
            Logger.LogInfo("Loading QuestingBots...");
            Singleton<LoggingUtil>.Create(new LoggingUtil(Logger));

            Logger.LogInfo("Loading QuestingBots...getting configuration data...");
            Singleton<ConfigUtil>.Create(new ConfigUtil());
            if (Singleton<ConfigUtil>.Instance.CurrentConfig == null)
            {
                Chainloader.DependencyErrors.Add("Could not load " + ModInfo.MODNAME + " because it cannot communicate with the server. Please ensure the mod has been installed correctly.");
                return;
            }

            new Patches.MenuShowPatch().Enable();

            EnableMod();

            Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...done.");
        }

        private void EnableMod()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.IsModEnabled())
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...enabling patches...");

            EnableCommonPatches();
            EnableLighthousePatches();
            
            EnableSpawningPatches();
            EnablePlayerScavGenerationPatches();
            RegisterBotGenerators();

            EnableDebugPatches();

            // Add options to the F12 menu
            QuestingBotsPluginConfig.BuildConfigOptions(Config);
        }

        private void EnableCommonPatches()
        {
            new Patches.TarkovInitPatch().Enable();
            new Patches.BotsControllerInitPatch().Enable();
            new Patches.BotsControllerStopPatch().Enable();
            new Patches.BotOwnerBrainActivatePatch().Enable();
            new Patches.IsFollowerSuitableForBossPatch().Enable();
            new Patches.OnBeenKilledByAggressorPatch().Enable();
            new Patches.AirdropLandPatch().Enable();
            new Patches.ServerRequestPatch().Enable();
            new Patches.CheckLookEnemyPatch().Enable();
            new Patches.ReturnToPoolPatch().Enable();
            new Patches.BotOwnerSprintPatch().Enable();
            new Patches.DisableLocalAvoidancePatch().Enable();
        }

        private void EnableLighthousePatches()
        {
            new Patches.Lighthouse.MineDirectionalShouldExplodePatch().Enable();
            new Patches.Lighthouse.LighthouseTraderZoneAwakePatch().Enable();
            new Patches.Lighthouse.LighthouseTraderZonePlayerAttackPatch().Enable();
        }

        private void EnableDebugPatches()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.IsDebugEnabled())
            {
                return;
            }

            new Patches.DebugPatches.ProcessSourceOcclusionPatch().Enable();
            //new Patches.DebugPatches.HandleFinishedTaskPatch().Enable();
            //new Patches.DebugPatches.HandleFinishedTaskPatch2().Enable();
        }

        private void EnableSpawningPatches()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Loading QuestingBots...enabling patches...enabling spawning patches");

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

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.SpawnInitialBossesFirst)
            {
                new Patches.Spawning.InitBossSpawnLocationPatch().Enable();
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PMCHostilityAdjustments.Enabled && Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
            {
                new Patches.Spawning.BotsGroupIsPlayerEnemyPatch().Enable();
            }
        }

        private void RegisterBotGenerators()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return;
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PMCs.Enabled)
            {
                BotGenerator.RegisterBotGenerator<Components.Spawning.PMCGenerator>();
                Singleton<LoggingUtil>.Instance.LogInfo("Enabled PMC bot generation");
            }
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PScavs.Enabled)
            {
                BotGenerator.RegisterBotGenerator<Components.Spawning.PScavGenerator>(true);
                Singleton<LoggingUtil>.Instance.LogInfo("Enabled PScav bot generation");
            }
        }

        private void EnablePlayerScavGenerationPatches()
        {
            if (!ShouldEnablePlayerScavGenerationPatches())
            {
                return;
            }

            new Patches.PScavProfilePatch().Enable();
        }

        private bool ShouldEnablePlayerScavGenerationPatches()
        {
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled && Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.PScavs.Enabled)
            {
                return true;
            }

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.AdjustPScavChance.Enabled)
            {
                return true;
            }

            return false;
        }
    }
}
