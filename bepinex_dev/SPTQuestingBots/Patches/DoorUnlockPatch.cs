using Aki.Reflection.Patching;
using EFT.Interactive;
using EFT.UI;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class DoorUnlockPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(NavMeshDoorLink).GetMethod("method_0", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(NavMeshDoorLink __instance, WorldInteractiveObject obj, EDoorState prevstate, EDoorState nextstate)
        {
            LoggingController.LogInfo("Object " + obj.Id + ": CanBeCarved=" + __instance.CanBeCarved);
        }
    }
}
