using System.Reflection;
using UnityEngine;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
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
            if (gameObject.TryGetComponent<BotLogic.Objective.BotObjectiveManager>(out var objectiveManager))
            {
                LoggingController.LogDebug("Destroying BotObjectiveManager component.");
                UnityEngine.Object.Destroy(objectiveManager);
            }
        }
    }
}
