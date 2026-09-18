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
    public class TeleportDebuggingPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotMover).GetMethod(nameof(BotMover.Teleport), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static void PatchPrefix(Vector3 rPosition, BotMover __instance, BotOwner ____owner)
        {
            if (!____owner.IsUsingQuestingBotsBrainLayer())
            {
                return;
            }

            float distance = Vector3.Distance(____owner.Position, rPosition);
            Singleton<LoggingUtil>.Instance.LogDebug(____owner.GetText() + " will teleport " + distance + "m");

            StackTrace stackTrace = new StackTrace();
            Singleton<LoggingUtil>.Instance.LogDebug(stackTrace.ToString());
        }
    }
}
