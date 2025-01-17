using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using SPT.Reflection.Patching;

namespace SPTQuestingBots.Patches
{
    public class SetNewBossPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BossGroup).GetMethod("method_0", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BossGroup __instance, BotOwner boss, List<BotOwner> followers)
        {
            LoggingController.LogInfo("Checking for a new follower from [" + string.Join(", ", followers) + "] to replace " + boss.GetText());
            LoggingController.LogInfo("Current boss: " + __instance.Boss?.GetText() ?? "[None]");
        }
    }
}
