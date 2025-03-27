using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
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
                //LoggingController.LogInfo("Disabling " + debugController.GetType().FullName + "...");
                debugController.enabled = false;
            }

            // Disable all bot generators
            foreach (Components.Spawning.BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(Components.Spawning.BotGenerator)))
            {
                //LoggingController.LogInfo("Disabling " + botGenerator.GetType().FullName + "...");
                botGenerator.enabled = false;
            }

            // Write all log files
            if (Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                long timestamp = DateTime.Now.ToFileTimeUtc();

                BotJobAssignmentFactory.WriteQuestLogFile(timestamp);
                BotJobAssignmentFactory.WriteBotJobAssignmentLogFile(timestamp);
            }

            // Erase all bot and bot-assignment tracking data
            BotJobAssignmentFactory.Clear();
            Controllers.BotRegistrationManager.Clear();

            // Not really needed since BotHiveMindMonitor is attached to GameWorld, but this may reduce CPU load a tad
            BotLogic.HiveMind.BotHiveMindMonitor.Clear();
        }
    }
}
