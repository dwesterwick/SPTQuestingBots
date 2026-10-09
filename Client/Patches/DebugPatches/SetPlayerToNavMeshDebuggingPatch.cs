using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace QuestingBots.Patches.DebugPatches
{
    public class SetPlayerToNavMeshDebuggingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotMover).GetMethod(nameof(BotMover.SetPlayerToNavMesh), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(Vector3 castPoint, EBotLinkResult __result, BotMover __instance, BotOwner ____owner)
        {
            if ((__result == EBotLinkResult.complete) || (__result == EBotLinkResult.extraConnect))
            {
                return;
            }

            if (!____owner.IsUsingQuestingBotsBrainLayer())
            {
                return;
            }

            float distance = Vector3.Distance(____owner.Position, castPoint);
            Singleton<LoggingUtil>.Instance.LogWarning("SetPlayerToNavMesh for " + ____owner.GetText() + " returned " + __result.ToString());
        }
    }
}
