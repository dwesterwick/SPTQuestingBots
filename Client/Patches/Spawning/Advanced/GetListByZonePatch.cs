using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using QuestingBots.Helpers;

namespace QuestingBots.Patches.Spawning.Advanced
{
    public class GetListByZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsList).GetMethod(nameof(BotsList.GetListByZone), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref List<BotOwner> __result, BotZone zone)
        {
            List<BotOwner> remainingBots = new List<BotOwner>();
            foreach (BotOwner bot in __result)
            {
                if (bot.ShouldPlayerBeTreatedAsHuman())
                {
                    continue;
                }

                remainingBots.Add(bot);
            }

            __result = remainingBots;
        }
    }
}
