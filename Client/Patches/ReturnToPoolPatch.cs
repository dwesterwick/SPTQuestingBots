using System.Reflection;
using UnityEngine;
using SPT.Reflection.Patching;
using EFT.AssetsManager;
using System;

namespace QuestingBots.Patches
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
            TryDestroyComponent<Components.BotIdentityData>(gameObject);
            TryDestroyComponent<Components.BotObjectiveManager>(gameObject);
            TryDestroyComponent<BotLogic.BotMonitor.BotMonitorController>(gameObject);
        }

        private static void TryDestroyComponent<T>(GameObject gameObject) where T: Component
        {
            if (gameObject.TryGetComponent<T>(out var component))
            {
                UnityEngine.Object.Destroy(component);
            }
        }
    }
}
