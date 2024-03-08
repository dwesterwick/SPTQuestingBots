using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
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
        private static bool PatchPrefix(EnemyInfo __instance, GClass448 lookAll)
        {
            if (!QuestingBotsPluginConfig.SleepingEnabled.Value)
            {
                return true;
            }

            string botId = __instance?.Person?.ProfileId;
            if ((botId != null) && BotRegistrationManager.IsBotSleeping(botId))
            {
                __instance.SetVisible(false);
                return false;
            }

            return true;
        }
    }
}
