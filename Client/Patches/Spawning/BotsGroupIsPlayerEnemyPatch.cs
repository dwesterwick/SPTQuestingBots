using Comfort.Common;
using EFT;
using HarmonyLib;
using QuestingBots.Components.Spawning;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches.Spawning
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
            MethodInfo replacementMethodInfo = typeof(BotsGroupIsPlayerEnemyPatch).GetMethod(nameof(ShouldAlwaysBeAllies), BindingFlags.NonPublic | BindingFlags.Static);

            List<CodeInstruction> modifiedInstructions = originalInstructions.ToList();

            for (int i = 0; i < modifiedInstructions.Count; i++)
            {
                // Search for "if (this.IsSuitable(role))"
                if ((modifiedInstructions[i].opcode == OpCodes.Call) && ((MethodInfo)modifiedInstructions[i].operand == isSuitableMethodInfo))
                {
                    // Replace the role argument with player argument
                    modifiedInstructions[i - 1] = new CodeInstruction(OpCodes.Ldarg_1);

                    // Replace the original method call
                    modifiedInstructions[i] = new CodeInstruction(OpCodes.Call, replacementMethodInfo);
                }
            }

            return modifiedInstructions;
        }

        private static bool ShouldAlwaysBeAllies(BotsGroup __instance, IPlayer player)
        {
            if (__instance.InitialBot.Profile.Info.Side != EPlayerSide.Savage)
            {
                return false;
            }

            if (player.Profile.Info.Side != EPlayerSide.Savage)
            {
                return false;
            }

            bool isSuitable = __instance.IsSuitable(player.Profile.Info.Settings.Role);
            return isSuitable;
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotsGroup __instance, ref bool __result, IPlayer player)
        {
            // The transpiler patch only affects the result for AI players
            if (!player.IsAI)
            {
                return true;
            }

            if (BotGenerator.TryGetBotGroupFromAnyGenerator(__instance.InitialBot, out Models.BotSpawnInfo botSpawnInfo))
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
            if (!QuestingBotsPluginConfig.ShowHostilityDebugMessages.Value)
            {
                return;
            }

            if (!player.IsAI)
            {
                return;
            }

            if (__instance.InitialBot.Profile.Info.Settings.Role == player.Profile.Info.Settings.Role)
            {
                return;
            }

            if (!player.Profile.WillBeAPMC() && !player.Profile.WillBeAPlayerScav())
            {
                //return;
            }

            if (!__instance.InitialBot.WillBeAPMC() && !__instance.InitialBot.WillBeAPlayerScav())
            {
                //return;
            }

            string message = $"Group containing bot {__instance.InitialBot.GetText()} will be hostile toward bot {player.GetText()}: {__result}";
            if (!__result)
            {
                Singleton<LoggingUtil>.Instance.LogWarning(message);
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogInfo(message);
            }
        }
    }
}
