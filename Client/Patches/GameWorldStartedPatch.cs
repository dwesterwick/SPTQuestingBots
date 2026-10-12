using Comfort.Common;
using EFT;
using QuestingBots.Components;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace QuestingBots.Patches
{
    public class GameWorldStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(GameWorld __instance)
        {
            Singleton<GameWorld>.Instance.gameObject.GetOrAddComponent<DebugData>();
        }
    }
}
