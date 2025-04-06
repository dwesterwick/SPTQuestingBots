using System.Reflection;
using Comfort.Common;
using UnityEngine;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using EFT;

namespace SPTQuestingBots.Patches
{
    internal class BotExtractPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod(nameof(BaseLocalGame<EftGamePlayerOwner>.BotDespawn));
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotOwner botOwner)
        {
            LoggingController.LogDebug($"{botOwner.GetText()} extracted.");

            var botgame = Singleton<IBotGame>.Instance;
            botgame.BotsController.BotDied(botOwner);
            botgame.BotsController.DestroyInfo(botOwner.GetPlayer);
            Object.DestroyImmediate(botOwner.gameObject);
            Object.Destroy(botOwner);

            return false;
        }
    }
}
