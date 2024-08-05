using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class CheckLookEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EnemyInfo).GetMethod("CheckLookEnemy", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static bool PatchPrefix(EnemyInfo __instance, GClass521 lookAll)
        {
            if (!QuestingBotsPluginConfig.SleepingEnabled.Value)
            {
                return true;
            }

            IPlayer player = __instance?.Person;
            if (player != null)
            {
                if (BotRegistrationManager.IsBotSleeping(player.ProfileId))
                {
                    __instance.SetVisible(false);
                    return false;
                }

                bool botIsOnIsland = Singleton<GameWorld>.Instance.GetComponent<LocationData>().IsPointOnLightkeeperIsland(__instance.Owner.Position);
                bool enemyIsOnIsland = Singleton<GameWorld>.Instance.GetComponent<LocationData>().IsPointOnLightkeeperIsland(player.Position);

                if (botIsOnIsland && enemyIsOnIsland)
                {
                    LoggingController.LogWarning(__instance.Owner.GetText() + " is ignoring " + player.GetText() + " on Lightkeeper Island");
                    __instance.SetVisible(false);
                    return false;
                }
            }

            return true;
        }
    }
}
