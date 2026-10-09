using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches.DebugPatches
{
    public class OnItemTakenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotExternalItemsController).GetMethod(nameof(BotExternalItemsController.OnItemTaken), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotExternalItemsController __instance, Item obj, IPlayer itemLastOwner)
        {
            Singleton<LoggingUtil>.Instance.LogInfo(__instance._owner.GetText() + " picked up " + obj.LocalizedName() + " previously owned by " + itemLastOwner?.GetText() ?? "???");

            StackTrace stackTrace = new StackTrace();
            Singleton<LoggingUtil>.Instance.LogDebug(stackTrace.ToString());

            return true;
        }
    }
}
