using BepInEx.Bootstrap;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots
{
    internal class QuestingBotsBotGeneratorStatus
    {
        public bool IsValid { get; private set; } = false;
        public int RemainingBotGenerators { get; private set; } = -1;
        public int CurrentBotGeneratorProgress { get; private set; } = 0;
        public string CurrentBotGeneratorType { get; private set; } = null!;

        public QuestingBotsBotGeneratorStatus() { }

        public QuestingBotsBotGeneratorStatus(int remainingBotGenerators, int currentBotGeneratorProgress, string currentBotGeneratorType) : this()
        {
            RemainingBotGenerators = remainingBotGenerators;
            CurrentBotGeneratorProgress = currentBotGeneratorProgress;
            CurrentBotGeneratorType = currentBotGeneratorType;
            IsValid = true;
        }
    }

    internal class QuestingBotsBotQuestInfo
    {
        public bool IsValid { get; private set; } = false;
        public string CurrentDecision { get; private set; } = string.Empty;
        public string CurrentActionType { get; private set; } = string.Empty;
        public string QuestName { get; private set; } = string.Empty;
        public Vector3 QuestLocation { get; private set; } = Vector3.negativeInfinity;
        public bool IsEftQuest { get; private set; } = false;

        public bool HasAQuest => QuestName != string.Empty;

        public QuestingBotsBotQuestInfo() { }

        public QuestingBotsBotQuestInfo(string currentDecision, string currentActionType) : this()
        {
            CurrentDecision = currentDecision;
            CurrentActionType = currentActionType;
            IsValid = true;
        }

        public QuestingBotsBotQuestInfo(string currentDecision, string currentActionType, string questName, Vector3 questLocation, bool isEftQuest) : this(currentDecision, currentActionType)
        {
            QuestName = questName;
            QuestLocation = questLocation;
            IsEftQuest = isEftQuest;
        }
    }

    internal class QuestingBotsBotJobAssignmentHistoryEntry
    {
        public bool IsValid { get; private set; } = false;
        public string StartTimestampText { get; private set; } = string.Empty;
        public string EndTimestampText { get; private set; } = string.Empty;
        public string QuestName { get; private set; } = string.Empty;
        public string QuestObjectiveName { get; private set; } = string.Empty;
        public string QuestStep { get; private set; } = string.Empty;
        public string Status { get; private set; } = string.Empty;

        public long StartTimestamp => long.Parse(StartTimestampText);
        public long EndTimestamp => long.Parse(EndTimestampText);

        public QuestingBotsBotJobAssignmentHistoryEntry() { }

        public QuestingBotsBotJobAssignmentHistoryEntry(string startTimestamp, string endTimestamp, string questName, string questObjectiveName, string questStep, string status) : this()
        {
            StartTimestampText = startTimestamp;
            EndTimestampText = endTimestamp;
            QuestName = questName;
            QuestObjectiveName = questObjectiveName;
            QuestStep = questStep;
            Status = status;
            IsValid = true;
        }
    }

    internal static class QuestingBotsInterop
    {
        private static bool _QuestingBotsLoadedChecked = false;
        private static bool _QuestingBotsInteropInited = false;

        private static bool _IsQuestingBotsLoaded;
        private static Type _QuestingBotsExternalType = null!;

        private static MethodInfo _GetRemainingBotGeneratorsMethod = null!;
        private static MethodInfo _GetCurrentBotGeneratorProgressMethod = null!;
        private static MethodInfo _GetCurrentBotGeneratorTypeMethod = null!;
        private static MethodInfo _GetCurrentDecisionMethod = null!;
        private static MethodInfo _GetCurrentQuestActionTypeMethod = null!;
        private static MethodInfo _GetCurrentQuestNameMethod = null!;
        private static MethodInfo _GetCurrentQuestLocationMethod = null!;
        private static MethodInfo _IsCurrentJobAssignmentAnEftQuestMethod = null!;
        private static MethodInfo _IsCurrentJobAssignmentActiveMethod = null!;
        private static MethodInfo _HasAQuestingBossMethod = null!;
        private static MethodInfo _GetJobAssignmentHistoryCsvDataMethod = null!;

        /**
         * Return true if Questing Bots is loaded in the client
         */
        public static bool IsQuestingBotsLoaded()
        {
            // Only check for Questing Bots once
            if (!_QuestingBotsLoadedChecked)
            {
                _QuestingBotsLoadedChecked = true;
                _IsQuestingBotsLoaded = Chainloader.PluginInfos.ContainsKey("com.danw.questingbots");
            }

            return _IsQuestingBotsLoaded;
        }

        /**
         * Initialize the Questing Bots interop class data, return true on success
         */
        public static bool Init()
        {
            if (!IsQuestingBotsLoaded()) return false;

            // Only check for the External class once
            if (!_QuestingBotsInteropInited)
            {
                _QuestingBotsInteropInited = true;

                _QuestingBotsExternalType = Type.GetType("QuestingBots.QuestingBotsExternal, QuestingBots-Client");

                // Only try to get the methods if we have the type
                if (_QuestingBotsExternalType != null)
                {
                    _GetRemainingBotGeneratorsMethod = AccessTools.Method(_QuestingBotsExternalType, "GetRemainingBotGenerators");
                    _GetCurrentBotGeneratorProgressMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentBotGeneratorProgress");
                    _GetCurrentBotGeneratorTypeMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentBotGeneratorType");
                    _GetCurrentDecisionMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentDecision");
                    _GetCurrentQuestActionTypeMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentQuestActionType");
                    _GetCurrentQuestNameMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentQuestName");
                    _GetCurrentQuestLocationMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentQuestLocation");
                    _IsCurrentJobAssignmentAnEftQuestMethod = AccessTools.Method(_QuestingBotsExternalType, "IsCurrentJobAssignmentAnEftQuest");
                    _IsCurrentJobAssignmentActiveMethod = AccessTools.Method(_QuestingBotsExternalType, "HasActiveJobAssignment");
                    _HasAQuestingBossMethod = AccessTools.Method(_QuestingBotsExternalType, "HasAQuestingBoss");
                    _GetJobAssignmentHistoryCsvDataMethod = AccessTools.Method(_QuestingBotsExternalType, "GetJobAssignmentHistoryCsvData");
                }
            }

            // If we found the External class, at least some of the methods are (probably) available
            return (_QuestingBotsExternalType != null);
        }

        /**
         * Return the status of the currently (or most recently) running Questing Bots bot generator
         */
        public static QuestingBotsBotGeneratorStatus GetBotGeneratorStatus()
        {
            if (!Init()) return new QuestingBotsBotGeneratorStatus();
            if (_GetRemainingBotGeneratorsMethod == null) return new QuestingBotsBotGeneratorStatus();
            if (_GetCurrentBotGeneratorProgressMethod == null) return new QuestingBotsBotGeneratorStatus();
            if (_GetCurrentBotGeneratorTypeMethod == null) return new QuestingBotsBotGeneratorStatus();

            int remainingGenerators = (int)_GetRemainingBotGeneratorsMethod.Invoke(null, new object[] { });
            int currentGeneratorProgress = (int)_GetCurrentBotGeneratorProgressMethod.Invoke(null, new object[] { });
            string currentGeneratorType = (string)_GetCurrentBotGeneratorTypeMethod.Invoke(null, new object[] { });

            return new QuestingBotsBotGeneratorStatus(remainingGenerators, currentGeneratorProgress, currentGeneratorType);
        }

        /**
         * Return the all current questing information for the specified bot
         */
        public static QuestingBotsBotQuestInfo GetBotQuestInfo(BotOwner bot)
        {
            if (!Init()) return new QuestingBotsBotQuestInfo();
            if (_GetCurrentDecisionMethod == null) return new QuestingBotsBotQuestInfo();
            if (_GetCurrentQuestActionTypeMethod == null) return new QuestingBotsBotQuestInfo();
            if (_GetCurrentQuestNameMethod == null) return new QuestingBotsBotQuestInfo();
            if (_GetCurrentQuestLocationMethod == null) return new QuestingBotsBotQuestInfo();
            if (_IsCurrentJobAssignmentAnEftQuestMethod == null) return new QuestingBotsBotQuestInfo();
            if (_IsCurrentJobAssignmentActiveMethod == null) return new QuestingBotsBotQuestInfo();
            if (_HasAQuestingBossMethod == null) return new QuestingBotsBotQuestInfo();

            string decision = (string)_GetCurrentDecisionMethod.Invoke(null, new object[] { bot });
            string actionType = (string)_GetCurrentQuestActionTypeMethod.Invoke(null, new object[] { bot });

            bool hasActiveJob = (bool)_IsCurrentJobAssignmentActiveMethod.Invoke(null, new object[] { bot });
            bool hasAQuestingBoss = (bool)_HasAQuestingBossMethod.Invoke(null, new object[] { bot });
            if (!hasActiveJob || hasAQuestingBoss)
            {
                return new QuestingBotsBotQuestInfo(decision, actionType);
            }

            string questName = (string)_GetCurrentQuestNameMethod.Invoke(null, new object[] { bot });
            Vector3 questLocation = (Vector3)_GetCurrentQuestLocationMethod.Invoke(null, new object[] { bot });
            bool isEftQuest = (bool)_IsCurrentJobAssignmentAnEftQuestMethod.Invoke(null, new object[] { bot });

            return new QuestingBotsBotQuestInfo(decision, actionType, questName, questLocation, isEftQuest);
        }

        /**
         * Return the current questing decision for the specified bot
         */
        public static string GetCurrentDecision(BotOwner bot)
        {
            if (!Init()) return "";
            if (_GetCurrentDecisionMethod == null) return "";

            string decision = (string)_GetCurrentDecisionMethod.Invoke(null, new object[] { bot });
            return decision;
        }

        /**
         * Return the questing action currently being performed by the specified bot
         */
        public static string GetCurrentQuestActionType(BotOwner bot)
        {
            if (!Init()) return "";
            if (_GetCurrentQuestActionTypeMethod == null) return "";

            string actionType = (string)_GetCurrentQuestActionTypeMethod.Invoke(null, new object[] { bot });
            return actionType;
        }

        /**
         * Return the name of the quest currently being performed by the specified bot
         */
        public static string GetCurrentQuestName(BotOwner bot)
        {
            if (!Init()) return "";
            if (_GetCurrentQuestNameMethod == null) return "";

            string questName = (string)_GetCurrentQuestNameMethod.Invoke(null, new object[] { bot });
            return questName;
        }

        /**
         * Return the location of the quest currently being performed by the specified bot
         */
        public static Vector3 GetCurrentQuestLocation(BotOwner bot)
        {
            if (!Init()) return Vector3.negativeInfinity;
            if (_GetCurrentQuestLocationMethod == null) return Vector3.negativeInfinity;

            Vector3 questLocation = (Vector3)_GetCurrentQuestLocationMethod.Invoke(null, new object[] { bot });
            return questLocation;
        }

        /**
         * Return if the quest currently being performed by the specified bot (if applicable) is an EFT quest
         */
        public static bool IsCurrentJobAssignmentAnEftQuest(BotOwner bot)
        {
            if (!Init()) return false;
            if (_IsCurrentJobAssignmentAnEftQuestMethod == null) return false;

            bool isEftQuest = (bool)_IsCurrentJobAssignmentAnEftQuestMethod.Invoke(null, new object[] { bot });
            return isEftQuest;
        }

        /**
         * Return if the specified bot has an active job assignment (with Pending or Active status)
         */
        public static bool HasAnActiveJobAssignment(BotOwner bot)
        {
            if (!Init()) return false;
            if (_IsCurrentJobAssignmentActiveMethod == null) return false;

            bool hasActiveJobAssignment = (bool)_IsCurrentJobAssignmentActiveMethod.Invoke(null, new object[] { bot });
            return hasActiveJobAssignment;
        }

        /**
         * Return if the specified bot has a boss that is questing
         */
        public static bool HasAQuestingBoss(BotOwner bot)
        {
            if (!Init()) return false;
            if (_HasAQuestingBossMethod == null) return false;

            bool hasAQuestingBoss = (bool)_HasAQuestingBossMethod.Invoke(null, new object[] { bot });
            return hasAQuestingBoss;
        }

        /**
         * Return information about all job assignments for the specified bot
         */
        public static IEnumerable<QuestingBotsBotJobAssignmentHistoryEntry> GetJobAssignmentHistory(BotOwner bot)
        {
            if (!Init()) yield break;
            if (_GetJobAssignmentHistoryCsvDataMethod == null) yield break;

            IEnumerable<string[]> historyCsvEntries = (IEnumerable<string[]>)_GetJobAssignmentHistoryCsvDataMethod.Invoke(null, new object[] { bot });
            foreach (string[] historyCsvEntry in historyCsvEntries)
            {
                QuestingBotsBotJobAssignmentHistoryEntry historyEntry = new QuestingBotsBotJobAssignmentHistoryEntry
                (
                    historyCsvEntry[0],
                    historyCsvEntry[1],
                    historyCsvEntry[2],
                    historyCsvEntry[3],
                    historyCsvEntry[4],
                    historyCsvEntry[5]
                );

                yield return historyEntry;
            }
        }
    }
}
