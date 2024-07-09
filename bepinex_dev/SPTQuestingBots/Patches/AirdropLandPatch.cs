﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Custom.Airdrops;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;

namespace SPTQuestingBots.Patches
{
    internal class AirdropLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AirdropBox).GetMethod("ReleaseAudioSource", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(AirdropBox __instance)
        {
            Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().AddAirdropChaserQuest(__instance.transform.position);
        }
    }
}
