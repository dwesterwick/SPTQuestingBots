using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using QuestingBots.Controllers;
using SPT.Reflection.Patching;
using QuestingBots.Helpers;
using Comfort.Common;
using QuestingBots.Utils;

namespace QuestingBots.Patches.Spawning
{
    public class SetNewBossPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            MethodInfo methodInfo = typeof(BossGroup)
                .GetMethods()
                .First(m => m.IsUnmapped() && m.HasAllParameterTypesInOrder(new Type[] { typeof(BotOwner) }));

            Singleton<LoggingUtil>.Instance.LogInfo("Found method for SetNewBossPatch: " + methodInfo.Name);

            return methodInfo;
        }

        [PatchPrefix]
        protected static void PatchPrefix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, BotOwner ___Boss_1)
        {
            foreach (BotOwner follower in followers)
            {
                follower.BotFollower.BossToFollow = null;
            }
        }

        [PatchPostfix]
        protected static void PatchPostfix(BossGroup __instance, BotOwner boss, List<BotOwner> followers, ref BotOwner ___Boss_1)
        {
            ___Boss_1 = null!;

            foreach (BotOwner follower in followers)
            {
                if (follower.Boss.IamBoss && (follower.Profile.Id != boss.Profile.Id))
                {
                    ___Boss_1 = follower;
                }
            }

            if ((___Boss_1 == null) && (followers.Count > 1))
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Could not find a new boss to replace " + boss.GetText());
            }
        }
    }
}
