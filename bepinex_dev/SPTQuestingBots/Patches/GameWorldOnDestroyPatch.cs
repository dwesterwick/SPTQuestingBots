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
using SPTQuestingBots.Controllers.Bots;
using SPTQuestingBots.Controllers.Bots.Spawning;

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
            if (Singleton<GameWorld>.Instance.gameObject.TryGetComponent(out DebugController debugController))
            {
                debugController.enabled = false;
            }

            // Don't do anything if this is for the hideout
            if (!Singleton<GameWorld>.Instance.GetComponent<LocationController>().HasRaidStarted)
            {
                return;
            }

            // Write all log files
            if (Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>().HaveQuestsBeenBuilt)
            {
                long timestamp = DateTime.Now.ToFileTimeUtc();

                BotJobAssignmentFactory.WriteQuestLogFile(timestamp);
                BotJobAssignmentFactory.WriteBotJobAssignmentLogFile(timestamp);
            }

            // Erase all bot and bot-assignment tracking data
            BotJobAssignmentFactory.Clear();
            Controllers.Bots.Spawning.BotRegistrationManager.Clear();

            // Not really needed since BotHiveMindMonitor is attached to GameWorld, but this may reduce CPU load a tad
            BotLogic.HiveMind.BotHiveMindMonitor.Clear();
        }
    }
}
