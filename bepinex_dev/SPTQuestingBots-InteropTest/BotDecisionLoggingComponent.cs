using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SPTQuestingBotsInteropTest;
using UnityEngine;

namespace SPTQuestingBots_InteropTest
{
    public class BotDecisionLoggingComponent : MonoBehaviour
    {
        private const int UPDATE_DELAY = 5000;
        private const string NO_DECISION_TEXT = "None";

        private Stopwatch updateTimer = Stopwatch.StartNew();

        protected void Update()
        {
            if (!Singleton<IBotGame>.Instantiated)
            {
                return;
            }

            if (updateTimer.ElapsedMilliseconds < UPDATE_DELAY)
            {
                return;
            }

            updateTimer.Restart();

            foreach (BotOwner bot in Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners)
            {
                logBotDecision(bot);
            }
        }

        private static void logBotDecision(BotOwner bot)
        {
            string botDecision = SPTQuestingBots.QuestingBotsInterop.GetCurrentDecision(bot);
            if (string.IsNullOrEmpty(botDecision) || (botDecision == NO_DECISION_TEXT))
            {
                return;
            }

            LoggingController.LogInfo($"Current decision for {bot.name}: {botDecision}");
        }
    }
}
