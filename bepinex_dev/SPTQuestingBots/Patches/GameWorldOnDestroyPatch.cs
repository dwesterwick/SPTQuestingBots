using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace SPTQuestingBots.Patches
{
    public class GameWorldOnDestroyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod("OnDestroy", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(GameWorld __instance)
        {
            // Don't do anything if this is for the hideout
            if (!Controllers.LocationController.HasRaidStarted)
            {
                return;
            }

            // Needed for compatibility with Refringe's CustomRaidTimes mod
            Controllers.LocationController.ClearEscapeTimes();

            BotLogic.HiveMind.BotHiveMindMonitor.Clear();
        }
    }
}
