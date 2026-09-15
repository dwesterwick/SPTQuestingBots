using Comfort.Common;
using EFT;
using HarmonyLib;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace QuestingBots.Patches
{
    public class OverrideLocalAvoidancePowerLimitPatch : ModulePatch
    {
        public static float MaxDistToOfBypassOverride => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.RepulsionPowerLimit;

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotLocalAvoidance).GetMethod(nameof(BotLocalAvoidance.AddPower), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchTranspiler]
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            MethodInfo MaxDistToOfBypassMethod = AccessTools.Method(typeof(OverrideLocalAvoidancePowerLimitPatch), nameof(GetMaxDistToOfBypass));

            foreach (CodeInstruction originalInstruction in originalInstructions)
            {
                if ((originalInstruction.opcode == OpCodes.Ldc_R4) && ((float)originalInstruction.operand == BotLocalAvoidance.MaxDistToOfBypass))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, MaxDistToOfBypassMethod);

                    continue;
                }

                yield return originalInstruction;
            }
        }

        private static float GetMaxDistToOfBypass(BotLocalAvoidance __instance)
        {
            return ShouldUseOverrides(__instance._owner) ? MaxDistToOfBypassOverride : BotLocalAvoidance.MaxDistToOfBypass;
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
    }
}
