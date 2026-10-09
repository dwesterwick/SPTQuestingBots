using EFT;
using HarmonyLib;
using QuestingBots.Helpers;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace QuestingBots.Patches
{
    public class PatrolStatusSetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(PatrollingData).GetProperty("Status", BindingFlags.Public | BindingFlags.Instance).GetSetMethod();
        }

        [PatchTranspiler]
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            MethodInfo originalStopMethod = AccessTools.Method(typeof(BotOwner), nameof(BotOwner.StopMove));
            MethodInfo newStopMethod = AccessTools.Method(typeof(PatrolStatusSetPatch), nameof(stopIfNotQuesting));

            foreach (CodeInstruction originalInstruction in originalInstructions)
            {
                if ((originalInstruction.opcode == OpCodes.Callvirt) && ((MethodInfo)originalInstruction.operand == originalStopMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, newStopMethod);
                    continue;
                }

                yield return originalInstruction;
            }
        }

        private const string LOOTING_BOTS_LAYER_NAME = "Looting";
        private static void stopIfNotQuesting(BotOwner bot)
        {
            if (bot.IsUsingQuestingBotsBrainLayer())
            {
                return;
            }

            if ((bot.GetActiveLayerName() == LOOTING_BOTS_LAYER_NAME) && bot.IsAllowedToQuest())
            {
                return;
            }

            bot.StopMove();
        }
    }
}
