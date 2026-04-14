using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using HarmonyLib;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace QuestingBots.Patches
{
    public class TriggerZoneBotPoliticPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TriggerZone).GetMethod(nameof(TriggerZone.method_1), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchTranspiler]
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions, ILGenerator iLGenerator)
        {
            MethodInfo playerShouldSetOffAlarmMethodInfo = typeof(TriggerZoneBotPoliticPatch).GetMethod(nameof(PlayerShouldSetOffAlarm), BindingFlags.NonPublic | BindingFlags.Static);
            MethodInfo skipReturnInILHelperMethodInfo = typeof(TriggerZoneBotPoliticPatch).GetMethod(nameof(SkipReturnInILHelper), BindingFlags.NonPublic | BindingFlags.Static);
            Label continueLabel = iLGenerator.DefineLabel();

            List<CodeInstruction> modifiedInstructions = new List<CodeInstruction>();
            
            bool modificationPerformed = false;
            foreach (CodeInstruction originalInstruction in originalInstructions)
            {
                if (modificationPerformed)
                {
                    modifiedInstructions.Add(originalInstruction);
                    continue;
                }

                // We only want to replace "return;" in the following:
                // if (player.IsAI && this._botPolitic == TriggerZone.EBotPolitic.Ignore)
                // {
                //     return;
                // }
                if (originalInstruction.opcode != OpCodes.Ret)
                {
                    modifiedInstructions.Add(originalInstruction);
                    continue;
                }

                modifiedInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1));
                modifiedInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                modifiedInstructions.Add(new CodeInstruction(OpCodes.Call, playerShouldSetOffAlarmMethodInfo));
                modifiedInstructions.Add(new CodeInstruction(OpCodes.Call, skipReturnInILHelperMethodInfo));

                modificationPerformed = true;
            }

            return modifiedInstructions;
        }

        private static void SkipReturnInILHelper(bool shouldSkipReturn)
        {
            if (!shouldSkipReturn)
            {
                // This will add OpCodes.Ret into the IL stream
                return;
            }
        }

        private static bool PlayerShouldSetOffAlarm(Player player, TriggerZone zone)
        {
            LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();
            if (!locationData.IsTriggerZoneForAlarm(zone))
            {
                return false;
            }

            if (!player.ShouldPlayerBeTreatedAsHuman())
            {
                return false;
            }

            Singleton<LoggingUtil>.Instance.LogDebug(player.GetText() + " is allowed to set off alarm linked to " + zone.transform.parent.name + "::" + zone.name);

            return true;
        }
    }
}
