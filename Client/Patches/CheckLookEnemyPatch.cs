using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using QuestingBots.Controllers;

namespace QuestingBots.Patches
{
    public class CheckLookEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(EnemyInfo).GetMethod(nameof(EnemyInfo.CheckLookEnemy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(EnemyInfo __instance)
        {
            if (!QuestingBotsPluginConfig.SleepingEnabled.Value)
            {
                return true;
            }

            if (__instance == null)
            {
                return true;
            }

            IPlayer player = __instance.Person;
            if (player == null)
            {
                return true;
            }

            if (BotRegistrationManager.IsBotSleeping(player.ProfileId))
            {
                __instance.SetVisible(false);
                return false;
            }

            return true;
        }
    }
}
