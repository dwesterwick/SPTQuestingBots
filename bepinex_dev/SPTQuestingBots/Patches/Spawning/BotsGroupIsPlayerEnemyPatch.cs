using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches.Spawning
{
    public class BotsGroupIsPlayerEnemyPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsGroup).GetMethod(nameof(BotsGroup.IsPlayerEnemy), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchTranspiler]
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            MethodInfo isSuitableMethodInfo = typeof(BotsGroup).GetMethod(nameof(BotsGroup.IsSuitable), BindingFlags.Public | BindingFlags.Instance);

            List<CodeInstruction> modifiedInstructions = originalInstructions.ToList();

            for (int i = 0; i < modifiedInstructions.Count; i++)
            {
                // Search for "if (this.IsSuitable(role))"
                if ((modifiedInstructions[i].opcode == OpCodes.Call) && ((MethodInfo)modifiedInstructions[i].operand == isSuitableMethodInfo))
                {
                    // Remove the "return false" inside the "if" block
                    for (int j = i + 2; j < i + 4; j++)
                    {
                        modifiedInstructions[j].opcode = OpCodes.Nop;
                        modifiedInstructions[j].operand = null;
                    }
                }
            }

            return modifiedInstructions;
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotsGroup __instance, bool __result, IPlayer player)
        {
            if ((__instance._initialBot.Profile.Info.Settings.Role != WildSpawnType.pmcBEAR) && (__instance._initialBot.Profile.Info.Settings.Role != WildSpawnType.pmcUSEC))
            {
                return;
            }

            if ((player.Profile.Info.Settings.Role != WildSpawnType.pmcBEAR) && (player.Profile.Info.Settings.Role != WildSpawnType.pmcUSEC))
            {
                return;
            }

            string message = "[BotsGroup.IsPlayerEnemy]" + player.GetText() + "(" + player.Profile.Info.Settings.Role + ") is enemy of " + __instance._initialBot.Profile.Info.Settings.Role + " group: " + __result;

            if (__result)
            {
                //LoggingController.LogInfo(message);
            }
            else
            {
                LoggingController.LogWarning(message);
            }
        }
    }
}
