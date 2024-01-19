using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix()
        {
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out Components.DebugData debugController))
            {
                LoggingController.LogInfo("Disabling " + nameof(debugController) + "...");
                debugController.enabled = false;
            }

            foreach (Components.Spawning.BotGenerator botGenerator in Singleton<GameWorld>.Instance.gameObject.GetComponents(typeof(Components.Spawning.BotGenerator)))
            {
                LoggingController.LogInfo("Disabling " + nameof(botGenerator) + "...");
                botGenerator.enabled = false;
            }

            // Don't do anything if this is for the hideout
            if (!Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().HasRaidStarted)
            {
                return;
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
