using Comfort.Common;
using EFT;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Utils.Benchmarking;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches
{
    public class BotsControllerStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(nameof(BotsController.StopGettingInfo), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix()
        {
            // Stop updating debug overlays
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out Components.DebugData debugController))
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Disabling " + debugController.GetType().FullName + "...");
                debugController.enabled = false;
            }

            RemoveCommonGameWorldComponents();

            // Disable all bot generators
            foreach (Components.Spawning.BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(Components.Spawning.BotGenerator)))
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Disabling " + botGenerator.GetType().FullName + "...");
                botGenerator.enabled = false;
            }

            // Write all log files
            if (Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                long timestamp = DateTime.Now.ToFileTimeUtc();

                BotJobAssignmentController.WriteQuestLogFile(timestamp);
                BotJobAssignmentController.WriteBotJobAssignmentLogFile(timestamp);

#if DEBUG
                BenchmarkService.LogAllBenchmarksAndReset(timestamp);
#endif
            }

            // Erase all bot and bot-assignment tracking data
            BotJobAssignmentController.Clear();
            Controllers.BotRegistrationManager.Clear();

            // Not really needed since BotHiveMindMonitor is attached to GameWorld, but this may reduce CPU load a tad
            BotLogic.HiveMind.BotHiveMindMonitor.Clear();
        }

        public static void RemoveCommonGameWorldComponents()
        {
            if (Singleton<PlayerData>.Instance != null)
            {
                Singleton<PlayerData>.Release(Singleton<PlayerData>.Instance);
            }
        }
    }
}
