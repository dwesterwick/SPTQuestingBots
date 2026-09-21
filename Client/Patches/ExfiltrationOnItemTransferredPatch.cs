using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    internal class ExfiltrationOnItemTransferredPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(ExfiltrationPoint).GetMethod(
                nameof(ExfiltrationPoint.OnItemTransferred),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(IPlayer) },
                null);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ExfiltrationPoint __instance)
        {
            if (!Singleton<ConfigUtil>.Instance.CarExtractNames.Contains(__instance.Settings.Name))
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("VEX started");
        }
    }
}
