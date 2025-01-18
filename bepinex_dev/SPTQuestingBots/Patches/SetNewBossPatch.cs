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

        [PatchPrefix]
        protected static void PatchPrefix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, BotOwner ____boss)
        {
            LoggingController.LogInfo("Checking for a new follower from [" + string.Join(", ", followers.Select(f => f.GetText())) + "] to replace " + boss.GetText());
            LoggingController.LogInfo("Old boss: " + ____boss.GetText());

            foreach (BotOwner follower in followers)
            {
                follower.BotFollower.BossToFollow = null;
            }
        }

        [PatchPostfix]
        protected static void PatchPostfix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, BotOwner ____boss)
        {
            foreach (BotOwner follower in followers)
            {
                if (follower.Boss.IamBoss)
                {
                    ____boss = follower;
                }
            }

            LoggingController.LogInfo("New boss: " + ____boss.GetText());
        }
    }
}
