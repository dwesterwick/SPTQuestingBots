using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    internal class WildSpawnTypeChangerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass732).GetMethod("method_3", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(BotZone zone, BotOwner bot, Action<BotOwner> callback, Func<BotOwner, BotZone, BotsGroup> groupAction)
        {
            // PMC groups are automatically converted to "assaultGroup" wildSpawnTypes by EFT, so they need to be changed back for the SPT PMC patch to work
            if (bot.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                TryConvertSpawnType(bot);
            }
        }

        public static bool TryConvertSpawnType(BotOwner __instance)
        {
            if (!Controllers.Bots.BotGenerator.TryGetInitialPMCGroup(__instance, out Models.BotSpawnInfo groupData))
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

            LoggingController.LogInfo("Converted spawn type for bot " + __instance.GetText() + " from " + currentRoleName + " to " + actualRoleName);

            return true;
        }
    }
}
