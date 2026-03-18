using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using UnityEngine;
using QuestingBots.Utils;

namespace QuestingBots.Patches.Spawning
{
    public class GameStartPatch : ModulePatch
    {
        public static bool IsDelayingGameStart { get; set; } = false;

        private static readonly List<BossLocationSpawn> missedBossWaves = new List<BossLocationSpawn>();
        private static FieldInfo wavesSpawnScenarioField = null!;
        private static object localGameObj = null!;

        protected override MethodBase GetTargetMethod()
        {
            wavesSpawnScenarioField = AccessTools.Field(typeof(LocalGame), "wavesSpawnScenario_0");

            return typeof(BaseLocalGame<EftGamePlayerOwner>).GetMethod("vmethod_5", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(ref IEnumerator __result, object __instance)
        {
            if (!IsDelayingGameStart)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("The game start is not being delayed");
                return;
            }

            localGameObj = __instance;

            IEnumerator originalEnumeratorWithMessage = addDebugMessageAfterEnumerator(__result, "Original start-game IEnumerator completed");
            __result = new Models.EnumeratorCollection(originalEnumeratorWithMessage, waitForBotGenerators(), spawnMissedWaves());

            Singleton<LoggingUtil>.Instance.LogDebug("Injected wait-for-bot-gen IEnumerator into start-game IEnumerator");

            if (QuestingBotsPluginConfig.ShowSpawnDebugMessages.Value)
            {
                writeSpawnMessages();
            }
        }

        public static void ClearMissedWaves()
        {
            missedBossWaves.Clear();
        }

        public static void AddMissedBossWave(BossLocationSpawn wave)
        {
            missedBossWaves.Add(wave);
        }

        private static IEnumerator addDebugMessageAfterEnumerator(IEnumerator enumerator, string message)
        {
            yield return enumerator;
            Singleton<LoggingUtil>.Instance.LogDebug(message);
        }

        private static IEnumerator spawnMissedWaves()
        {
            IsDelayingGameStart = false;

            if (missedBossWaves.Any())
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Spawning missed boss waves...");

                foreach (BossLocationSpawn missedBossWave in missedBossWaves)
                {
                    Singleton<IBotGame>.Instance.BotsController.ActivateBotsByWave(missedBossWave);
                }
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Spawned all missed boss waves");

            yield return null;
        }

        private static IEnumerator waitForBotGenerators()
        {
            bool hadToWait = false;
            float waitIterationDuration = 100;

            while (BotGenerator.RemainingBotGenerators > 0)
            {
                if (!hadToWait)
                {
                    Singleton<LoggingUtil>.Instance.LogInfo("Waiting for " + BotGenerator.RemainingBotGenerators + " bot generators...");
                }
                hadToWait = true;

                yield return new WaitForSeconds(waitIterationDuration / 1000f);

                TimeHasComeScreenClassChangeStatusPatch.ChangeStatus("Generating " + BotGenerator.CurrentBotGeneratorType + "s", BotGenerator.CurrentBotGeneratorProgress / 100f);
            }

            if (hadToWait)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("All bot generators have finished.");
            }

            TimeHasComeScreenClassChangeStatusPatch.RestorePreviousStatus();
        }

        private static void writeSpawnMessages()
        {
            if (localGameObj as LocalGame == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Cannot write WavesSpawnScenario spawn messages for the current BaseLocalGame because it is not a LocalGame");

                return;
            }

            WavesSpawnScenario wavesSpawnScenario = (WavesSpawnScenario)wavesSpawnScenarioField.GetValue(localGameObj);
            if (wavesSpawnScenario?.SpawnWaves == null)
            {
                Singleton<LoggingUtil>.Instance.LogInfo("WavesSpawnScenario has no BotWaveDataClass waves");

                return;
            }

            foreach (BotWaveDataClass wave in wavesSpawnScenario.SpawnWaves.ToArray())
            {
                Singleton<LoggingUtil>.Instance.LogInfo("BotWaveDataClass at " + wave.Time + "s: " + wave.BotsCount + " bots of type " + wave.WildSpawnType.ToString());
            }
        }
    }
}
