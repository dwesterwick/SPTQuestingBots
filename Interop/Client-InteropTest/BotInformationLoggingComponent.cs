using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuestingBotsInteropTest;
using UnityEngine;
using QuestingBots;

namespace QuestingBots_InteropTest
{
    public class BotInformationLoggingComponent : MonoBehaviour
    {
        private const int UPDATE_DELAY = 5000;
        private const string NO_DECISION_TEXT = "None";
        private const string NO_ACTION_TEXT = "Undefined";

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
                LoggingController.LogInfo("--------------");
                logBotDecision(bot);
                logBotQuestingActionType(bot);
                logBotQuestName(bot);
                logBotQuestLocation(bot);
                logBotQuestIsEftQuest(bot);
                logBotHasActiveJobAssignment(bot);
                logBotHasAQuestingBoss(bot);
                LoggingController.LogInfo("--------------");
                logBotQuestInfo(bot);
                LoggingController.LogInfo("--------------");
            }
        }

        private static void logBotDecision(BotOwner bot)
        {
            string botDecision = QuestingBots.QuestingBotsInterop.GetCurrentDecision(bot);
            if (string.IsNullOrEmpty(botDecision) || (botDecision == NO_DECISION_TEXT))
            {
                return;
            }

            LoggingController.LogInfo($"Current decision for {bot.name}: {botDecision}");
        }

        private static void logBotQuestingActionType(BotOwner bot)
        {
            string botQuestingActionType = QuestingBots.QuestingBotsInterop.GetCurrentQuestActionType(bot);
            if (string.IsNullOrEmpty(botQuestingActionType) || (botQuestingActionType == NO_ACTION_TEXT))
            {
                return;
            }

            LoggingController.LogInfo($"Current questing action for {bot.name}: {botQuestingActionType}");
        }

        private static void logBotQuestName(BotOwner bot)
        {
            string botQuestName = QuestingBots.QuestingBotsInterop.GetCurrentQuestName(bot);
            if (string.IsNullOrEmpty(botQuestName))
            {
                return;
            }

            LoggingController.LogInfo($"Current quest for {bot.name}: {botQuestName}");
        }

        private static void logBotQuestLocation(BotOwner bot)
        {
            string botQuestName = QuestingBots.QuestingBotsInterop.GetCurrentQuestName(bot);
            if (string.IsNullOrEmpty(botQuestName))
            {
                return;
            }

            Vector3 botQuestLocation = QuestingBots.QuestingBotsInterop.GetCurrentQuestLocation(bot);

            LoggingController.LogInfo($"Current quest location for {bot.name}: {botQuestLocation}");
        }

        private static void logBotQuestIsEftQuest(BotOwner bot)
        {
            string botQuestName = QuestingBots.QuestingBotsInterop.GetCurrentQuestName(bot);
            if (string.IsNullOrEmpty(botQuestName))
            {
                return;
            }

            bool isEftQuest = QuestingBots.QuestingBotsInterop.IsCurrentJobAssignmentAnEftQuest(bot);

            LoggingController.LogInfo($"Current quest is an EFT quest for {bot.name}: {isEftQuest}");
        }

        private static void logBotHasActiveJobAssignment(BotOwner bot)
        {
            bool hasAnActiveJobAssignment = QuestingBots.QuestingBotsInterop.HasAnActiveJobAssignment(bot);

            LoggingController.LogInfo($"Current job assignment is active for {bot.name}: {hasAnActiveJobAssignment}");
        }

        private static void logBotHasAQuestingBoss(BotOwner bot)
        {
            bool hasAQuestingBoss = QuestingBots.QuestingBotsInterop.HasAQuestingBoss(bot);

            LoggingController.LogInfo($"Questing boss assigned for {bot.name}: {hasAQuestingBoss}");
        }

        private static void logBotQuestInfo(BotOwner bot)
        {
            QuestingBotsBotQuestInfo botQuestInfo = QuestingBots.QuestingBotsInterop.GetBotQuestInfo(bot);
            if ((botQuestInfo == null) || !botQuestInfo.IsValid)
            {
                return;
            }

            if ((botQuestInfo.CurrentDecision == NO_DECISION_TEXT) && (botQuestInfo.CurrentActionType == NO_ACTION_TEXT))
            {
                return;
            }

            LoggingController.LogInfo($"Current decision for {bot.name}: {botQuestInfo.CurrentDecision}");
            LoggingController.LogInfo($"Current questing action for {bot.name}: {botQuestInfo.CurrentActionType}");

            if (!botQuestInfo.HasAQuest)
            {
                return;
            }

            string eftQuestText = botQuestInfo.IsEftQuest ? "Is" : "Is not";

            LoggingController.LogInfo($"Current quest for {bot.name}: {botQuestInfo.QuestName} ({eftQuestText} an EFT quest)");
            LoggingController.LogInfo($"Current quest location for {bot.name}: {botQuestInfo.QuestLocation.ToString()}");
        }
    }
}
