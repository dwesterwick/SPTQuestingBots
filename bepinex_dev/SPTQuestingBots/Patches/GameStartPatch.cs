using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Patches
{
    public class GameStartPatch : ModulePatch
    {
        public static bool IsDelayingGameStart { get; set; } = false;

        private static readonly List<BotWaveDataClass> missedBotWaves = new List<BotWaveDataClass>();
        private static readonly List<BossLocationSpawn> missedBossWaves = new List<BossLocationSpawn>();
        private static object localGameObj = null;

        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            return localGameType.GetMethod("method_18", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref IEnumerator __result, object __instance, float startDelay)
        {
            if (!IsDelayingGameStart)
            {
                return;
            }

            localGameObj = __instance;

            __result = addIterationsToWaitForBotGenerators(__result);
            LoggingController.LogInfo("Injected wait-for-bot-gen IEnumerator into start-game IEnumerator");
        }

        public static void ClearMissedWaves()
        {
            missedBotWaves.Clear();
            missedBossWaves.Clear();
        }

        public static void AddMissedBotWave(BotWaveDataClass wave)
        {
            missedBotWaves.Add(wave);
        }

        public static void AddMissedBossWave(BossLocationSpawn wave)
        {
            missedBossWaves.Add(wave);
        }

        private static IEnumerator addIterationsToWaitForBotGenerators(IEnumerator originalTask)
        {
            float startTime = Time.time;
            float safetyDelay = 999;

            IEnumerable<object> timers = getAllTimers();

            //IEnumerable<float> originalTimerEndTimes = timers.Select(t => getTimerEndTime(t));
            //LoggingController.LogInfo("Original Start Time: " + startTime);
            //LoggingController.LogInfo("Original Timer EndTimes: " + string.Join(", ", originalTimerEndTimes));

            updateAllTimers(timers, 0, safetyDelay);

            yield return waitForBotGenerators();

            LoggingController.LogInfo("Injected wait-for-bot-gen IEnumerator completed");

            float newStartTime = Time.time;
            updateAllTimers(timers, newStartTime - startTime, -1 * safetyDelay);

            //IEnumerable<float> newTimerEndTimes = timers.Select(t => getTimerEndTime(t));
            //LoggingController.LogInfo("New Start Time: " + newStartTime);
            //LoggingController.LogInfo("New Timer EndTimes: " + string.Join(", ", newTimerEndTimes));

            IsDelayingGameStart = false;

            if (missedBotWaves.Any())
            {
                LoggingController.LogInfo("Spawning missed bot waves...");

                foreach (BotWaveDataClass missedBotWave in missedBotWaves)
                {
                    Singleton<IBotGame>.Instance.BotsController.ActivateBotsByWave(missedBotWave);
                }
            }

            if (missedBossWaves.Any())
            {
                LoggingController.LogInfo("Spawning missed boss waves...");

                foreach (BossLocationSpawn missedBossWave in missedBossWaves)
                {
                    Singleton<IBotGame>.Instance.BotsController.ActivateBotsByWave(missedBossWave);
                }
            }

            yield return originalTask;

            LoggingController.LogInfo("Original start-game IEnumerator completed");
        }

        private static void updateAllTimers(IEnumerable<object> timers, float delay, float safetyDelay)
        {
            foreach (object timer in timers)
            {
                float currentEndTime = getTimerEndTime(timer);
                float newEndTime = currentEndTime + delay + safetyDelay;

                MethodInfo restartMethod = AccessTools.Method(timer.GetType(), "Restart");
                restartMethod.Invoke(timer, new object[] { newEndTime });
            }

            LoggingController.LogInfo("Added additional delay of " + delay + "s to " + timers.Count() + " timers");
        }

        private static float getTimerEndTime(object timer)
        {
            PropertyInfo endTimeProperty = AccessTools.Property(timer.GetType(), "EndTime");
            float endTime = (float)endTimeProperty.GetValue(timer);

            return endTime;
        }

        private static IEnumerable<object> getAllTimers()
        {
            List<object> timers = new List<object>();

            FieldInfo linkedListField = AccessTools.Field(StaticManager.Instance.TimerManager.GetType(), "linkedList_0");
            ICollection linkedList = (ICollection)linkedListField.GetValue(StaticManager.Instance.TimerManager);

            LoggingController.LogInfo("Found Timer Manager LinkedList (" + linkedList.Count + " timers)");

            foreach (var timer in linkedList)
            {
                timers.Add(timer);
            }

            FieldInfo wavesSpawnScenarioField = AccessTools.Field(Aki.Reflection.Utils.PatchConstants.LocalGameType, "wavesSpawnScenario_0");
            WavesSpawnScenario wavesSpawnScenario = (WavesSpawnScenario)wavesSpawnScenarioField.GetValue(localGameObj);

            //LoggingController.LogInfo("Found WavesSpawnScenario instance");

            FieldInfo wavesSpawnScenarioTimersField = AccessTools.Field(typeof(WavesSpawnScenario), "list_0");
            ICollection wavesSpawnScenarioTimers = (ICollection)wavesSpawnScenarioTimersField.GetValue(wavesSpawnScenario);

            LoggingController.LogInfo("Found WavesSpawnScenario timers (" + wavesSpawnScenarioTimers.Count + " timers)");

            foreach (var timer in wavesSpawnScenarioTimers)
            {
                timers.Add(timer);
            }

            FieldInfo bossWavesField = AccessTools.Field(Aki.Reflection.Utils.PatchConstants.LocalGameType, "gclass503_0");
            GClass503 bossWaves = (GClass503)bossWavesField.GetValue(localGameObj);

            //LoggingController.LogInfo("Found Boss Waves instance");

            FieldInfo bossWavesTimersField = AccessTools.Field(typeof(GClass503), "list_0");
            ICollection bossWavesTimers = (ICollection)bossWavesTimersField.GetValue(bossWaves);

            LoggingController.LogInfo("Found Boss Waves timers (" + bossWavesTimers.Count + " timers)");

            foreach (var timer in bossWavesTimers)
            {
                timers.Add(timer);
            }

            FieldInfo questTriggerField = AccessTools.Field(typeof(GClass503), "gclass504_0");
            GClass504 questTrigger = (GClass504)questTriggerField.GetValue(bossWaves);

            //LoggingController.LogInfo("Found Boss Waves Quest Trigger instance");

            FieldInfo questTriggerTimerField = AccessTools.Field(typeof(GClass504), "ginterface8_0");
            object questTriggerTimer = questTriggerTimerField.GetValue(questTrigger);

            if (questTriggerTimer != null)
            {
                LoggingController.LogInfo("Found Boss Waves Quest Trigger timer");

                timers.Add(questTriggerTimer);
            }

            return timers;
        }

        private static IEnumerator waitForBotGenerators()
        {
            bool hadToWait = false;
            float waitIterationDuration = 100;
            int maxPeriodsInText = 5;

            int periods = 1;
            while (BotGenerator.RemainingBotGenerators > 0)
            {
                if (!hadToWait)
                {
                    LoggingController.LogInfo("Waiting for " + BotGenerator.RemainingBotGenerators + " bot generators...");
                }
                hadToWait = true;

                yield return new WaitForSeconds(waitIterationDuration / 1000f);

                string message = "Generating " + BotGenerator.CurrentBotGeneratorType + "s (" + BotGenerator.CurrentBotGeneratorProgress + "%)";
                MatchmakerFinalCountdownUpdatePatch.SetText(message + (new string('.', periods)));

                periods++;
                if (periods > maxPeriodsInText)
                {
                    periods = 1;
                }
            }

            if (hadToWait)
            {
                LoggingController.LogInfo("All bot generators have finished.");
            }

            MatchmakerFinalCountdownUpdatePatch.ResetText();
        }
    }
}
