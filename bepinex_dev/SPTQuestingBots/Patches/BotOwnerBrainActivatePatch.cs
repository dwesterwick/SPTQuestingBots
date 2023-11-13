using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerBrainActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("method_10", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            registerBot(__instance);
        }

        private static void registerBot(BotOwner __instance)
        {
            string roleName = __instance.Profile.Info.Settings.Role.ToString();

            LoggingController.LogInfo("Initial spawn type for bot " + __instance.GetText() + ": " + roleName);
            if (Controllers.Bots.BotBrainHelpers.WillBotBeAPMC(__instance))
            {
                Controllers.Bots.BotRegistrationManager.RegisterPMC(__instance);
            }

            if (Controllers.Bots.BotBrainHelpers.WillBotBeABoss(__instance))
            {
                Controllers.Bots.BotRegistrationManager.RegisterBoss(__instance);
            }

            BotLogic.HiveMind.BotHiveMindMonitor.RegisterBot(__instance);
        }
    }
}
