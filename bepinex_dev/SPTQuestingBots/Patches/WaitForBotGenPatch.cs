using Aki.Reflection.Patching;
using EFT;
using EFT.Communications;
using HarmonyLib;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Patches
{
    public class WaitForBotGenPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(TarkovApplication).GetMethod("method_34", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        [PatchPostfix]
        private static void PatchPostfix(ref Task __result)
        {
            return;

            Task originalTask = __result;
            __result = addTask(originalTask);

            LoggingController.LogInfo("Injected bot-gen task into start-game task");
        }

        private static async Task addTask(Task originalTask)
        {
            await originalTask;
            await waitForBotGen();

            LoggingController.LogInfo("Completed injected bot-gen task within start-game task");
        }

        private static async Task waitForBotGen()
        {
            bool hadToWait = false;
            int notificationUpdatePeriod = 2000;

            while (BotGenerator.RemainingBotGenerators > 0)
            {
                hadToWait = true;

                string message = "Waiting for " + BotGenerator.RemainingBotGenerators + " bot generators to finish...";

                LoggingController.LogInfo(message);
                NotificationManagerClass.DisplayMessageNotification(message, ENotificationDurationType.Default, ENotificationIconType.Default, Color.white);

                await Task.WhenAny(BotGenerator.BotGenerationTask, Task.Delay(notificationUpdatePeriod));
            }

            if (hadToWait)
            {
                LoggingController.LogInfo("All bot generators have finished.");
            }
        }
    }
}
