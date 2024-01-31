using Aki.Reflection.Patching;
using EFT;
using EFT.Communications;
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
            yield return waitForBotGen();
            yield return originalTask;
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
                    string message = "Waiting for " + BotGenerator.RemainingBotGenerators + " bot generators to finish...";

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
