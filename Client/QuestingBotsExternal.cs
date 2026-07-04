using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using QuestingBots.Models.Questing;
using UnityEngine;

namespace QuestingBots
{
    internal static class QuestingBotsExternal
    {
        public static int GetRemainingBotGenerators() => BotGenerator.RemainingBotGenerators;
        public static int GetCurrentBotGeneratorProgress() => BotGenerator.CurrentBotGeneratorProgress;
        public static string GetCurrentBotGeneratorType() => BotGenerator.CurrentBotGeneratorType;

        public static string GetCurrentDecision(this BotOwner bot)
        {
            BotQuestingDecision defaultDecision = BotQuestingDecision.None;

            BotObjectiveManager? botObjectiveManager = bot.GetBotObjectiveManagerForActiveBot();
            if ((botObjectiveManager == null) || (botObjectiveManager.BotMonitor == null))
            {
                return defaultDecision.ToString();
            }

            return botObjectiveManager.BotMonitor.CurrentDecision.ToString();
        }

        public static string GetCurrentQuestActionType(this BotOwner bot)
        {
            BotActionType defaultActionType = BotActionType.Undefined;

            BotObjectiveManager? botObjectiveManager = bot.GetBotObjectiveManagerForActiveBot();
            if (botObjectiveManager == null)
            {
                return defaultActionType.ToString();
            }

            return botObjectiveManager.CurrentQuestAction.ToString();
        }

        public static string GetCurrentQuestName(this BotOwner bot)
        {
            BotJobAssignment? botJobAssignment = bot.GetCurrentJobAssignmentForActiveBot();
            if ((botJobAssignment?.QuestAssignment == null) || botJobAssignment.IsCompletedOrArchived)
            {
                return string.Empty;
            }

            return botJobAssignment.QuestAssignment.Name;
        }

        public static bool IsCurrentJobAssignmentAnEftQuest(this BotOwner bot)
        {
            BotJobAssignment? botJobAssignment = bot.GetCurrentJobAssignmentForActiveBot();
            if ((botJobAssignment == null) || !botJobAssignment.IsActive())
            {
                return false;
            }

            return botJobAssignment.QuestAssignment.IsEFTQuest;
        }

        public static Vector3 GetCurrentQuestLocation(this BotOwner bot)
        {
            BotJobAssignment? botJobAssignment = bot.GetCurrentJobAssignmentForActiveBot();
            if ((botJobAssignment == null) || !botJobAssignment.IsActive())
            {
                return Vector3.negativeInfinity;
            }

            if (botJobAssignment.Position.HasValue)
            {
                return botJobAssignment.Position.Value;
            }

            return Vector3.negativeInfinity;
        }

        public static bool HasActiveJobAssignment(this BotOwner bot)
        {
            BotJobAssignment? botJobAssignment = bot.GetCurrentJobAssignmentForActiveBot();
            if ((botJobAssignment == null) || !botJobAssignment.IsActive())
            {
                return false;
            }

            return true;
        }

        public static bool HasAQuestingBoss(this BotOwner bot)
        {
            BotObjectiveManager? botObjectiveManager = bot.GetBotObjectiveManagerForActiveBot();
            if ((botObjectiveManager == null) || (botObjectiveManager.BotMonitor == null))
            {
                return false;
            }

            return botObjectiveManager.BotMonitor.HasAQuestingBoss;
        }

        private static BotObjectiveManager? GetBotObjectiveManagerForActiveBot(this BotOwner bot)
        {
            if (!bot.IsActive())
            {
                return null;
            }

            return bot.GetObjectiveManager();
        }

        private static BotJobAssignment? GetCurrentJobAssignmentForActiveBot(this BotOwner bot)
        {
            if (!bot.IsActive())
            {
                return null;
            }

            BotJobAssignment? botJobAssignment = BotJobAssignmentFactory.GetCurrentJobAssignment(bot, false);
            return botJobAssignment;
        }

        private static bool IsActive(this BotOwner? bot)
        {
            return (bot != null) && (bot.BotState == EBotState.Active) && !bot.IsDead;
        }

        private static bool IsActive(this BotJobAssignment botJobAssignment)
        {
            return (botJobAssignment.QuestAssignment != null) && botJobAssignment.IsActive;
        }
    }
}
