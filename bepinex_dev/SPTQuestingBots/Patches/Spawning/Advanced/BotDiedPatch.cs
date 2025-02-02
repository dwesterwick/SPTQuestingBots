using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning.Advanced
{
    public class BotDiedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotSpawner).GetMethod(nameof(BotSpawner.BotDied), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner bot, BotsClass ____bots, Action<BotOwner> ___OnBotRemoved)
        {
            if (!bot.ShouldPlayerBeTreatedAsHuman())
            {
                return true;
            }

            bot.IsDead = true;

            ____bots.Remove(bot);

            if (___OnBotRemoved != null)
            {
                ___OnBotRemoved(bot);
            }

            return false;
        }
    }
}
