using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class BotOwnerSprintPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotOwner).GetMethod(nameof(BotOwner.Sprint), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner __instance, bool val, bool withDebugCallback)
        {
            Components.BotObjectiveManager botObjectiveManager = __instance.GetObjectiveManager();
            if (botObjectiveManager != null)
            {
                botObjectiveManager.BotSprintingController.ExternalUpdate(val, withDebugCallback);
                return false;
            }

            return true;
        }
    }
}
