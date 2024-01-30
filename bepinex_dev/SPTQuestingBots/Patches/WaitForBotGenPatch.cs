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
            __result = __result.ContinueWith(async (_) =>
            {
                await waitForBotGen();
            }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());

            LoggingController.LogInfo("Injected bot-gen task into start-game task");
        }

        private static async Task waitForBotGen()
        {
            bool hadToWait = false;
            int minWaitTime = 50;
            int notificationUpdatePeriod = 1000;

            //IReadOnlyCollection<Task> botgenerationTasks = BotGenerator.GetBotGeneratorCreators();
            //while (botgenerationTasks.Any(t => !t.IsCompleted))
            while (BotGenerator.RemainingBotGenerators > 0)
            {
                hadToWait = true;

                //int remainingTasks = botgenerationTasks.Count(t => !t.IsCompleted);
                //string message = "Waiting for (" + remainingTasks + "/" + botgenerationTasks.Count + ") bot generators to finish...";
                string message = "Waiting for " + BotGenerator.RemainingBotGenerators + " bot generators to finish...";

                LoggingController.LogInfo(message);
                NotificationManagerClass.DisplayMessageNotification(message, ENotificationDurationType.Default, ENotificationIconType.Default, Color.white);

                IEnumerable<Task> whenAllTasks = BotGenerator.GetBotGeneratorTasks().AddItem(Task.Delay(minWaitTime));
                await Task.WhenAny(Task.WhenAll(whenAllTasks), Task.Delay(notificationUpdatePeriod));
                //await Task.WhenAny(BotGenerator.botGenerationMonitorTask, Task.Delay(notificationUpdatePeriod));
            }

            if (hadToWait)
            {
                LoggingController.LogInfo("All bot generators have finished.");
            }
        }
    }
}
