using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using SPTQuestingBots.BotLogic;
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
            string roleName = __instance.Profile.Info.Settings.Role.ToString();

            if (__instance.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                if (TryConvertSpawnType(__instance))
                {
                    roleName = __instance.Profile.Info.Settings.Role.ToString();
                }
            }

            LoggingController.LogInfo("Initial spawn type for bot " + __instance.Profile.Nickname + ": " + roleName);
            if (BotBrainHelpers.WillBotBeAPMC(__instance))
            {
                BotQuestController.RegisterPMC(__instance);
            }

            if (BotBrainHelpers.WillBotBeABoss(__instance))
            {
                BotQuestController.RegisterBoss(__instance);
            }

            BotHiveMindMonitor.RegisterBot(__instance);
        }

        private static bool TryConvertSpawnType(BotOwner __instance)
        {
            if (!BotGenerator.TryGetInitialPMCGroup(__instance, out Models.BotSpawnInfo groupData))
            {
                return false;
            }

            WildSpawnType? originalSpawnType = groupData.GetOriginalSpawnTypeForBot(__instance);
            if (originalSpawnType == null)
            {
                return false;
            }

            string currentRoleName = __instance.Profile.Info.Settings.Role.ToString();

            __instance.Profile.Info.Settings.Role = originalSpawnType.Value;

            string actualRoleName = __instance.Profile.Info.Settings.Role.ToString();

            LoggingController.LogInfo("Converted spawn type for bot " + __instance.Profile.Nickname + " from " + currentRoleName + " to " + actualRoleName);

            return true;
        }
    }
}
