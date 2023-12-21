using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Custom.Airdrops;
using Aki.Reflection.Patching;

namespace SPTQuestingBots.Patches
{
    internal class OnBoxLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("OnBoxLand", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(AirdropBox __instance)
        {
            Controllers.Bots.BotQuestBuilder.AddAirdropChaserQuest(__instance.transform.position);
        }
    }
}
