using System;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using QuestingBots.BotLogic.ExternalMods;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using UnityEngine;

namespace QuestingBots.Patches.Spawning
{
    /// <summary>
    /// This patch ensures the game start is delayed for all Fika clients until all BotGenerators have finished.
    /// </summary>
    /// <remarks>
    /// This patch injects <see cref="WaitForBotGenerators"/> to Fika's <c>HeadlessGame.RunMemoryCleanup</c> Task,
    /// which runs before <c>HeadlessGameController.WaitForHeadlessInit</c>,
    /// which sends the packet to all players that they may proceed with the countdown.
    /// </remarks>
    public class HeadlessGameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return ExternalModHandler.HeadlessModInfo.RunMemoryCleanupMethod;
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref Task __result, object __instance)
        {
            if (!GameStartPatch.IsDelayingGameStart)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("The game start is not being delayed");
                return;
            }

            __result = WaitForBotGenerators(__result, __instance);
            Singleton<LoggingUtil>.Instance.LogDebug("Injected wait-for-bot-gen Task into Headless run-memory-cleanup Task");

            if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                GameStartPatch.WriteSpawnMessages(__instance);
            }
        }

        private static async Task WaitForBotGenerators(Task originalTask, object gameObj)
        {
            if (!(gameObj is MonoBehaviour game))
            {
                Singleton<LoggingUtil>.Instance.LogWarning($"The game start is not being delayed. Unexpected Type for HeadlessGame: {gameObj.GetType().FullName}");
                return;
            }

            await originalTask;
            Singleton<LoggingUtil>.Instance.LogDebug("Original run-memory-cleanup Task completed");

            TaskCompletionClass source = new TaskCompletionClass();
            game.StartCoroutine(GameStartPatch.WaitForBotGenerators(source.Complete));
            await source.Task;

            GameStartPatch.SpawnMissedWaves();
        }
    }
}
