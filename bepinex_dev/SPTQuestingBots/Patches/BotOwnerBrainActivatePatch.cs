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

        // Only used if Questing Bots manages PMC spawns
        [PatchPrefix]
        private static void PatchPrefix(BotOwner __instance)
        {
            if (ConfigController.Config.InitialPMCSpawns.Enabled)
            {
                registerBot(__instance);
            }
        }

        // Only used if another mod (i.e. SWAG+DONUTS) manages PMC spawns
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            if (!ConfigController.Config.InitialPMCSpawns.Enabled)
            {
                registerBot(__instance);
            }
        }

        private static void registerBot(BotOwner __instance)
        {
            string roleName = __instance.Profile.Info.Settings.Role.ToString();

            // PMC groups are automatically converted to "assaultGroup" wildSpawnTypes by EFT, so they need to be changed back for the SPT PMC patch to work
            if (__instance.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                if (TryConvertSpawnType(__instance))
                {
                    roleName = __instance.Profile.Info.Settings.Role.ToString();
                }
            }

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

        private static bool TryConvertSpawnType(BotOwner __instance)
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
