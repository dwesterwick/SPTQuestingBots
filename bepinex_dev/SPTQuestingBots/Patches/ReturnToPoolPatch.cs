using System.Reflection;
using UnityEngine;
using SPT.Reflection.Patching;
using EFT.AssetsManager;
using System;

namespace SPTQuestingBots.Patches
{
    internal class ReturnToPoolPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(AssetPoolObject).GetMethod(nameof(AssetPoolObject.ReturnToPool), new Type[] { typeof(GameObject), typeof(bool) });
        }

        [PatchPrefix]
        protected static void PatchPrefix(GameObject gameObject)
        {
            if (gameObject.TryGetComponent<Components.BotObjectiveManager>(out var objectiveManager))
            {
                UnityEngine.Object.Destroy(objectiveManager);
            }
        }
    }
}
