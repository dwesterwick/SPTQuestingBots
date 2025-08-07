using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches.Spawning
{
    public class BotsGroupIsPlayerEnemyPatch : ModulePatch
    {
        private static readonly bool SHOW_DEBUG_MESSAGES = false;

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

        [PatchPrefix]
        protected static bool PatchPrefix(BotsGroup __instance, ref bool __result, IPlayer player)
        {
            // The transpiler patch only affects the result for AI players
            if (!player.IsAI)
            {
                return true;
            }

            if (BotGenerator.TryGetBotGroupFromAnyGenerator(__instance._initialBot, out Models.BotSpawnInfo botSpawnInfo))
            {
                if (botSpawnInfo.ContainsProfile(player.Profile))
                {
                    // Ensure group members are not enemies
                    __result = false;
                    return false;
                }
            }

            return true;
        }

        [PatchPostfix]
        protected static void PatchPostfix(BotsGroup __instance, bool __result, IPlayer player)
        {
            if (!SHOW_DEBUG_MESSAGES)
            {
                return;
            }

            if (!player.IsAI)
            {
                return;
            }

            if (__instance._initialBot.Profile.Info.Settings.Role == player.Profile.Info.Settings.Role)
            {
                return;
            }

            if (!player.Profile.WillBeAPMC() && !player.Profile.WillBeAPlayerScav())
            {
                return;
            }

            if (!__instance._initialBot.WillBeAPMC() && !__instance._initialBot.WillBeAPlayerScav())
            {
                return;
            }

            string message = $"Group containing bot {__instance._initialBot.GetText()} will be hostile toward bot {player.GetText()}: {__result}";
            if (!__result)
            {
                LoggingController.LogWarning(message);
            }
            else
            {
                LoggingController.LogInfo(message);
            }
        }
    }
}
