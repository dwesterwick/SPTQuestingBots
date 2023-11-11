using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;

namespace SPTQuestingBots.Patches
{
    internal class WildSpawnTypeChangerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GClass732).GetMethod("method_3", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(BotZone zone, BotOwner bot, Action<BotOwner> callback, Func<BotOwner, BotZone, BotsGroup> groupAction)
        {
            // PMC groups are automatically converted to "assaultGroup" wildSpawnTypes by EFT, so they need to be changed back for the SPT PMC patch to work
            if (bot.Profile.Info.Settings.Role == WildSpawnType.assaultGroup)
            {
                //Controllers.Bots.BotBrainHelpers.TryConvertSpawnType(bot);
            }
        }
    }
}
