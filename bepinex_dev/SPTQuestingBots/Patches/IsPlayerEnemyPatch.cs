using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class IsPlayerEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsGroup).GetMethod("IsPlayerEnemy", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(bool __result, BotsGroup __instance, IPlayer player)
        {
            List<BotOwner> groupMembers = SPT.Custom.CustomAI.AiHelpers.GetAllMembers(__instance);

            LoggingController.LogInfo("IsPlayerEnemy -> " + player.GetText() + " for group containing [" + string.Join(", ", groupMembers.Select(m => m.GetText())) + "]: " + __result);
        }
    }
}
