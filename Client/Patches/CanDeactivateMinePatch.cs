using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Patches
{
    public class CanDeactivateMinePatch : ModulePatch
    {
        private struct LastUpdateInfo
        {
            public float Time;
            public bool State;

            public LastUpdateInfo(float time, bool state)
            {
                Time = time;
                State = state;
            }
        }

        private const float UPDATE_DELAY = 0.2f;

        private static Dictionary<BotOwner, LastUpdateInfo> lastUpdateInfoForBotOwners = new Dictionary<BotOwner, LastUpdateInfo>();

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotBewarePlantedMine).GetMethod(nameof(BotBewarePlantedMine.CanDeactivate), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotBewarePlantedMine __instance, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            if (!CanUpdate(__instance.BotOwner_0))
            {
                __result = GetLastValue(__instance.BotOwner_0);
                return;
            }

            // This is an expensive call and needs to be time-gated
            if (!HasCompletePath(__instance))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(__instance.BotOwner_0.GetText() + " does not have a complete path to deactivate nearby mine");
                __result = false;
            }

            UpdateLastValue(__instance.BotOwner_0, __result);
        }

        private static bool CanUpdate(BotOwner botOwner)
        {
            if (!lastUpdateInfoForBotOwners.ContainsKey(botOwner))
            {
                lastUpdateInfoForBotOwners.Add(botOwner, new LastUpdateInfo(Time.time, true));
                return true;
            }

            float timeSinceLastUpdate = Time.time - lastUpdateInfoForBotOwners[botOwner].Time;
            return timeSinceLastUpdate > UPDATE_DELAY;
        }

        private static bool GetLastValue(BotOwner botOwner)
        {
            if (!lastUpdateInfoForBotOwners.ContainsKey(botOwner))
            {
                Singleton<LoggingUtil>.Instance.LogError("CanDeactivateMinePatch LastUpdateInfo not set for " + botOwner.GetText());
                return true;
            }

            return lastUpdateInfoForBotOwners[botOwner].State;
        }

        private static bool HasCompletePath(BotBewarePlantedMine __instance)
        {
            NavMeshPathStatus pathStatus = BotPathingHelpers.CreatePathSegment(__instance.BotOwner_0.Position, __instance.DeactivatingPlace.Pos, out Vector3[] corners);
            return pathStatus == NavMeshPathStatus.PathComplete;
        }

        private static void UpdateLastValue(BotOwner botOwner, bool state)
        {
            LastUpdateInfo lastUpdateInfo = new LastUpdateInfo(Time.time, state);

            if (lastUpdateInfoForBotOwners.ContainsKey(botOwner))
            {
                lastUpdateInfoForBotOwners[botOwner] = lastUpdateInfo;
                return;
            }

            lastUpdateInfoForBotOwners.Add(botOwner, lastUpdateInfo);
        }
    }
}
