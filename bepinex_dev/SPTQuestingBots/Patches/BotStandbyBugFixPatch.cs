using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Patches
{
    public class BotStandbyBugFixPatch : ModulePatch
    {
        private static FieldInfo botOwnerField = AccessTools.Field(typeof(BotStandBy), "botOwner_0");
        private static FieldInfo curPointField = AccessTools.Field(typeof(BotStandBy), "_curPoint");

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotStandBy).GetMethod("UpdateNode", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(BotStandBy __instance)
        {
            BotOwner botOwner = (BotOwner)botOwnerField.GetValue(__instance);
            Vector3? curPoint = (Vector3?)curPointField.GetValue(__instance);

            if (botOwner == null)
            {
                LoggingController.LogError("BotStandby BotOwner is null");
                return;
            }

            if (botOwner.Covers == null)
            {
                LoggingController.LogError("BotStandby BotOwner Covers is null");
                return;
            }

            if (!curPoint.HasValue)
            {
                CustomNavigationPoint customNavigationPoint = botOwner.Covers.GetClosestPoint(botOwner.Position, null, true, false, 1000);

                if (customNavigationPoint == null)
                {
                    LoggingController.LogError("BotStandby CustomNavigationPoint is null");
                }
                else
                {
                    curPointField.SetValue(__instance, customNavigationPoint.Position);
                }
            }

            if (!curPoint.HasValue)
            {
                curPointField.SetValue(__instance, botOwner.Position);
            }
        }
    }
}
