using Comfort.Common;
using EFT;
using HarmonyLib;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    public class DisableLocalAvoidancePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localAvoidanceType = AccessTools.Field(typeof(BotMover), nameof(BotMover.LocalAvoidance)).FieldType;
            return localAvoidanceType.GetMethod("ManualUpdate", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner ___BotOwner_0)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.DisableEFTLocalAvoidance)
            {
                return true;
            }

            Components.BotObjectiveManager? objectiveManager = BotObjectiveManagerFactory.GetObjectiveManager(___BotOwner_0);
            if ((objectiveManager != null) && objectiveManager.IsQuestingAllowed)
            {
                return false;
            }

            return true;
        }
    }
}
