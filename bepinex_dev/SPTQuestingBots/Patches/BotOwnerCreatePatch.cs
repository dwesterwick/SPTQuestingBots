using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerCreatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPostfix]
        private static void PatchPostfix(BotOwner __result)
        {
            Controllers.LocationController.RegisterBot(__result);

            IReadOnlyCollection<BotOwner> friends = Controllers.BotGenerator.GetSpawnGroupMembers(__result);
            foreach (BotOwner friend in friends)
            {
                Player player = friend.GetPlayer;
                if (!__result.EnemiesController.IsEnemy(player))
                {
                    continue;
                }

                Controllers.LoggingController.LogInfo(friend.Profile.Nickname + " is now friends with " + __result.Profile.Nickname);
                __result.EnemiesController.Remove(player);
            }
        }
    }
}
