using System.Reflection;
using UnityEngine;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using EFT;

namespace SPTQuestingBots.Patches
{
    internal class BotDespawnPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.BotDespawn));
        }

        [PatchPrefix]
        protected static void PatchPrefix(BotOwner botOwner)
        {
            if (botOwner.GetPlayer.gameObject.TryGetComponent<BotLogic.Objective.BotObjectiveManager>(out var objectiveManager))
            {
                LoggingController.LogDebug($"{botOwner.GetText()} was despawned; destroying BotObjectiveManager component.");
                Object.Destroy(objectiveManager);
            }
        }
    }
}
