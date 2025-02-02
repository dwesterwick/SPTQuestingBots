using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using SPT.Reflection.Patching;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning
{
    public class SetNewBossPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(BossGroup)
                .GetMethods()
                .First(m => m.IsUnmapped() && m.HasAllParameterTypesInOrder(new Type[] { typeof(BotOwner) }));

            Controllers.LoggingController.LogInfo("Found method for SetNewBossPatch: " + methodInfo.Name);

            return methodInfo;
        }

        [PatchPrefix]
        protected static void PatchPrefix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, BotOwner ____boss)
        {
            foreach (BotOwner follower in followers)
            {
                follower.BotFollower.BossToFollow = null;
            }
        }

        [PatchPostfix]
        protected static void PatchPostfix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, ref BotOwner ____boss)
        {
            ____boss = null;

            foreach (BotOwner follower in followers)
            {
                if (follower.Boss.IamBoss && (follower.Profile.Id != boss.Profile.Id))
                {
                    ____boss = follower;
                }
            }

            if ((____boss == null) && (followers.Count > 1))
            {
                LoggingController.LogWarning("Could not find a new boss to replace " + boss.GetText());
            }
        }
    }
}
