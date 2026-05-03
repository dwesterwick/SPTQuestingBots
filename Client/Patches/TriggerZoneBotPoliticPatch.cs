using Comfort.Common;
using EFT;
using EFT.GameTriggers;
using HarmonyLib;
using QuestingBots.Components;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Custom.CustomAI;
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
        protected static IEnumerable<CodeInstruction> PatchTranspiler(IEnumerable<CodeInstruction> originalInstructions)
        {
            MethodInfo isAIMethodInfo = AccessTools.PropertyGetter(typeof(Player), nameof(Player.IsAI));
            MethodInfo shouldIgnorePlayerMethodInfo = AccessTools.Method(typeof(TriggerZoneBotPoliticPatch), nameof(ShouldIgnorePlayer));

            List<CodeInstruction> modifiedInstructions = new List<CodeInstruction>();
            
            foreach (CodeInstruction originalInstruction in originalInstructions)
            {
                // We only want to replace "player.IsAI" in the following with "ShouldIgnorePlayer(player, this)"
                // if (player.IsAI && this._botPolitic == TriggerZone.EBotPolitic.Ignore)
                // {
                //     return;
                // }
                if ((originalInstruction.opcode != OpCodes.Callvirt) || ((MethodInfo)originalInstruction.operand != isAIMethodInfo))
                {
                    modifiedInstructions.Add(originalInstruction);
                    continue;
                }

                //modifiedInstructions.Add(new CodeInstruction(OpCodes.Ldarg_1)); //<-- this should already be the previous instruction
                modifiedInstructions.Add(new CodeInstruction(OpCodes.Ldarg_0));
                modifiedInstructions.Add(new CodeInstruction(OpCodes.Call, shouldIgnorePlayerMethodInfo));
            }

            return modifiedInstructions;
        }

        private static bool ShouldIgnorePlayer(Player player, TriggerZone zone)
        {
            if (!player.IsAI)
            {
                return false;
            }

            BotOwner botOwner = player.GetBotOwner();
            if (botOwner == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not get BotOwner for " + player.GetText());
                return false;
            }

            // EFT behavior should only be changed for alarm TriggerZones
            LocationData locationData = Singleton<GameWorld>.Instance.GetComponent<LocationData>();
            if (!locationData.IsTriggerZoneForAlarm(zone))
            {
                return true;
            }

            if (botOwner.IsPMC() || botOwner.IsSimulatedPlayerScav())
            {
                return false;
            }

            return true;
        }
    }
}
