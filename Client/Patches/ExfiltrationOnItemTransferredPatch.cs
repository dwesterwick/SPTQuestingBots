using Comfort.Common;
using EFT;
using EFT.Interactive;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    internal class ExfiltrationOnItemTransferredPatch : ModulePatch
    {
        private static Stopwatch _debounceTimer = Stopwatch.StartNew();
        private static float _debounceTime = 0;

        private static double TimeSinceLastQuestAdded => _debounceTimer.ElapsedMilliseconds / 1000.0;

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
        protected static void PatchPostfix(ExfiltrationPoint __instance, IPlayer player)
        {
            // Do not run this on Fika client machines
            if (!Helpers.RaidHelpers.IsHostRaid())
            {
                return;
            }

            if (!Singleton<ConfigUtil>.Instance.CarExtractNames.Contains(__instance.Settings.Name))
            {
                return;
            }

            if (TimeSinceLastQuestAdded < _debounceTime)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Not enough time has elapsed since the previous VEX rush quest was added");
                return;
            }

            _debounceTime = __instance.Settings.ExfiltrationTime;
            Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().AddVexRushQuest(player.Position, _debounceTime);

            _debounceTimer.Restart();
        }
    }
}
