using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Patches.Spawning
{
    public class ActivateBotsByWavePatch : SpawnScenarioRunPatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(BotsController).GetMethod(
                nameof(BotsController.ActivateBotsByWave),
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(SpawnWave) },
                null);
        }

        [PatchPrefix]
        protected static bool PatchPrefix(BotsController __instance, ref Task __result, SpawnWave wave)
        {
            if (!RaidHelpers.ForcePScavs)
            {
                return true;
            }

            if ((wave.WildSpawnType != WildSpawnType.assault) && (wave.WildSpawnType != WildSpawnType.assaultGroup))
            {
                return true;
            }

            __result = DelayTaskUntilPScavGenerationFinishes(__instance, wave);
            return false;
        }

        private static async Task DelayTaskUntilPScavGenerationFinishes(BotsController __instance, SpawnWave wave)
        {
            await WaitForPScavGeneration();
            await __instance.ActivateBotsByWave(wave);
        }
    }

    public abstract class SpawnScenarioRunPatch : ModulePatch
    {
        private const string DEBUG_MESSAGE = "Waiting for PScav generation to finish before allowing EFT to generate assault Scavs...";

        protected static bool DebugMessageDisplayed = false;

        protected static async Task WaitForPScavGeneration()
        {
            while (RaidHelpers.ForcePScavs)
            {
                if (!DebugMessageDisplayed)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(DEBUG_MESSAGE);
                    DebugMessageDisplayed = true;
                }

                await Task.Delay(10);
            }

            if (DebugMessageDisplayed)
            {
                Singleton<LoggingUtil>.Instance.LogDebug(DEBUG_MESSAGE + "done.");
            }

            DebugMessageDisplayed = false;
        }
    }
}
