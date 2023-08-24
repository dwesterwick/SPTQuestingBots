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
    public class BotOwnerBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("method_10", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(BotOwner __instance)
        {
            Controllers.LoggingController.LogInfo("Initial spawn type for bot " + __instance.Profile.Nickname + ": " + __instance.Profile.Info.Settings.Role.ToString());
            if (BotLogic.BotBrains.WillBotBeAPMC(__instance))
            {
                Controllers.BotQuestController.RegisterPMC(__instance);
            }

            if (BotLogic.BotBrains.WillBotBeABoss(__instance))
            {
                Controllers.BotQuestController.RegisterBoss(__instance);
            }
        }
    }
}
