using Aki.Reflection.Patching;
using EFT;
using EFT.Communications;
using HarmonyLib;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static CW2.Animations.PhysicsSimulator.Val;

namespace SPTQuestingBots.Patches
{
    public class GameStartPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            Type localGameType = Aki.Reflection.Utils.PatchConstants.LocalGameType;
            return localGameType.GetMethod("method_18", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref IEnumerator __result, float startDelay)
        {
            __result = addTask(__result);
            LoggingController.LogInfo("Injected wait-for-bot-gen IEnumerator into start-game IEnumerator");
        }

        private static IEnumerator addTask(IEnumerator originalTask)
        {
            float safetyDelay = 999;
            updateAllTimers(0, safetyDelay);

            float startTime = Time.time;
            yield return waitForBotGen();

            LoggingController.LogInfo("Injected wait-for-bot-gen IEnumerator completed");

            updateAllTimers(Time.time - startTime, -1 * safetyDelay);

            yield return originalTask;

            LoggingController.LogInfo("Original start-game IEnumerator completed");
        }

        private static void updateAllTimers(float delay, float safetyDelay)
        {
            FieldInfo linkedListField = AccessTools.Field(StaticManager.Instance.TimerManager.GetType(), "linkedList_0");
            ICollection linkedList = (ICollection)linkedListField.GetValue(StaticManager.Instance.TimerManager);

            ArrayList linkedListCopy = new ArrayList(linkedList);
            foreach (var timer in linkedListCopy)
            {
                PropertyInfo endTimeProperty = AccessTools.Property(timer.GetType(), "EndTime");
                float currentEndTime = (float)endTimeProperty.GetValue(timer);
                float newEndTime = currentEndTime + delay + safetyDelay;

                //endTimeProperty.SetValue(timer, newEndTime);

                MethodInfo restartMethod = AccessTools.Method(timer.GetType(), "Restart");
                restartMethod.Invoke(timer, new object[] { newEndTime });
            }

            LoggingController.LogInfo("Added additional delay of " + delay + "s to " + linkedList.Count + " timers");
        }

        private static IEnumerator waitForBotGen()
        {
            bool hadToWait = false;
            float waitPeriod = 50;
            int notificationUpdatePeriod = 2000;

            Stopwatch notificationDelayTimer = Stopwatch.StartNew();
            while (BotGenerator.RemainingBotGenerators > 0)
            {
                if (!hadToWait || (notificationDelayTimer.ElapsedMilliseconds > notificationUpdatePeriod))
                {
                    string message = "Waiting for " + BotGenerator.RemainingBotGenerators + " bot generator(s) to finish...";

                    LoggingController.LogInfo(message);
                    NotificationManagerClass.DisplayMessageNotification(message, ENotificationDurationType.Default, ENotificationIconType.Default, Color.white);

                    notificationDelayTimer.Restart();
                }

                yield return new WaitForSeconds(waitPeriod / 1000f);
                hadToWait = true;
            }

            if (hadToWait)
            {
                LoggingController.LogInfo("All bot generators have finished.");
            }
        }
    }
}
