using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Controllers.Bots.Spawning;

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
            Controllers.Bots.Spawning.BotRegistrationManager.WriteMessageForNewBotSpawn(__result);

            // TO DO: Is the code below even needed?
            Singleton<GameWorld>.Instance.TryGetComponent(out PMCGenerator pmcGenerator);
            IReadOnlyCollection<BotOwner> friends = pmcGenerator?.GetSpawnGroupMembers(__result) ?? (new BotOwner[0]);
            foreach (BotOwner friend in friends)
            {
                Player player = friend.GetPlayer;
                if (!__result.EnemiesController.IsEnemy(player))
                {
                    continue;
                }

                Controllers.LoggingController.LogInfo(friend.GetText() + " is now friends with " + __result.GetText());
                __result.EnemiesController.Remove(player);
            }
        }
    }
}
