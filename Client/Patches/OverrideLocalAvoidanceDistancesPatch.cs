using Comfort.Common;
using EFT;
using HarmonyLib;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;

namespace QuestingBots.Patches
{
    public class OverrideLocalAvoidanceDistancesPatch : ModulePatch
    {
        private static float? _distToBeCloseOverride = null;
        public static float DistToBeCloseOverride
        {
            get
            {
                if (_distToBeCloseOverride == null)
                {
                    _distToBeCloseOverride = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AvoidanceRadius.Min;
                }

                return _distToBeCloseOverride.Value;
            }
        }

        private static float? _distToBeCloseExtOverride = null;
        public static float DistToBeCloseExtOverride
        {
            get
            {
                if (_distToBeCloseExtOverride == null)
                {
                    _distToBeCloseExtOverride = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AvoidanceRadius.Max;
                }

                return _distToBeCloseExtOverride.Value;
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotLocalAvoidance).GetMethod(nameof(BotLocalAvoidance.ManualUpdate), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchTranspiler]
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            MethodInfo DistToBeCloseExtMethod = AccessTools.Method(typeof(OverrideLocalAvoidanceDistancesPatch), nameof(GetDistToBeCloseExt));
            MethodInfo DistToBeCloseMethod = AccessTools.Method(typeof(OverrideLocalAvoidanceDistancesPatch), nameof(GetDistToBeClose));

            foreach (CodeInstruction originalInstruction in originalInstructions)
            {
                if ((originalInstruction.opcode == OpCodes.Ldc_R4) && ((float)originalInstruction.operand == BotLocalAvoidance.DistToBeCloseExt))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, DistToBeCloseExtMethod);

                    continue;
                }
                
                if ((originalInstruction.opcode == OpCodes.Ldc_R4) && ((float)originalInstruction.operand == BotLocalAvoidance.DistToBeClose))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, DistToBeCloseMethod);

                    continue;
                }

                yield return originalInstruction;
            }
        }

        private static float GetDistToBeCloseExt(BotLocalAvoidance __instance)
        {
            return ShouldUseOverrides(__instance._owner) ? DistToBeCloseExtOverride : BotLocalAvoidance.DistToBeCloseExt;
        }

        private static float GetDistToBeClose(BotLocalAvoidance __instance)
        {
            return ShouldUseOverrides(__instance._owner) ? DistToBeCloseOverride : BotLocalAvoidance.DistToBeClose;
        }

        private static bool ShouldUseOverrides(BotOwner bot)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AllowForQuestingBots)
            {
                return false;
            }

            if (!bot.IsAllowedToQuest())
            {
                return false;
            }

            if (!bot.IsUsingQuestingBotsBrainLayer())
            {
                return false;
            }

            return true;
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner ____owner)
        {
            if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AllowForQuestingBots)
            {
                return true;
            }

            return !____owner.IsAllowedToQuest();
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotLocalAvoidance __instance, BotOwner ____owner)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AllowForQuestingBots && ____owner.IsAllowedToQuest())
            {
                return;
            }

            if (__instance.NotLinkedOffset == Vector3.zero)
            {
                return;
            }

            //Singleton<LoggingUtil>.Instance.LogDebug("NotLinkedOffset offset is " + __instance.NotLinkedOffset.magnitude + " for " + ____owner.GetText());

            BotOwner ? nearestGroupMember = ____owner.GetNearestGroupMember(out float distance);
            if (distance > DistToBeCloseExtOverride * Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.RadiusMultiplierToDropOffset)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Dropping local avoidance offset for " + ____owner.GetText());
                __instance.DropOffset();
            }
        }
    }
}
