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

        [PatchPrefix]
        private static void PatchPrefix(BotOwner __instance)
        {
            string currentRoleName = __instance.Profile.Info.Settings.Role.ToString();
            string actualRoleName = currentRoleName;

            if (__instance.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                if (BotGenerator.TryGetInitialPMCProfile(__instance, out Profile profile))
                {
                    __instance.Profile.Info.Settings.Role = profile.Info.Settings.Role;
                    actualRoleName = __instance.Profile.Info.Settings.Role.ToString();

                    LoggingController.LogInfo("Converted spawn type for bot " + __instance.Profile.Nickname + " from " + currentRoleName + " to " + actualRoleName);
                }
            }

            LoggingController.LogInfo("Initial spawn type for bot " + __instance.Profile.Nickname + ": " + actualRoleName);
            if (BotBrainHelpers.WillBotBeAPMC(__instance))
            {
                BotQuestController.RegisterPMC(__instance);
            }

            if (BotBrainHelpers.WillBotBeABoss(__instance))
            {
                BotQuestController.RegisterBoss(__instance);
            }
        }
    }
}
