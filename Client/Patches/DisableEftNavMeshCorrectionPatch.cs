using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    public class DisableEftNavMeshCorrectionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotMover).GetMethod("method_12", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner ____owner)
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.EFTLocalAvoidance.AllowForQuestingBots)
            {
                return true;
            }

            if (!____owner.IsUsingQuestingBotsBrainLayer())
            {
                return true;
            }

            if (____owner.GetObjectiveManager()?.MightBeStuck == true)
            {
                return true;
            }

            return false;
        }
    }
}
