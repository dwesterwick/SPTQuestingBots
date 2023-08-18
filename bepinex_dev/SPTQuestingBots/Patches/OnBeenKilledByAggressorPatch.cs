using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace QuestingBots.Patches
{
    public class OnBeenKilledByAggressorPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(Player).GetMethod("OnBeenKilledByAggressor", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Player aggressor)
        {
            Controllers.LoggingController.LogInfo("Player " + __instance.Profile.Nickname + " was killed by " + aggressor.Profile.Nickname);
        }
    }
}
