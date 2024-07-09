﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches
{
    public class GetListByZonePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsClass).GetMethod("GetListByZone", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref List<BotOwner> __result, BotZone zone)
        {
            List<BotOwner> remainingBots = __result
                .Where(b => !b.ShouldPlayerBeTreatedAsHuman())
                .ToList();

            __result = remainingBots;
        }
    }
}
