﻿using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace SPTQuestingBots
{
    internal class QuestingBotsBotGeneratorStatus
    {
        public bool IsValid { get; private set; } = false;
        public int RemainingBotGenerators { get; private set; } = -1;
        public int CurrentBotGeneratorProgress { get; private set; } = 0;
        public string CurrentBotGeneratorType { get; private set; } = null;

        public QuestingBotsBotGeneratorStatus() { }

        public QuestingBotsBotGeneratorStatus(int remainingBotGenerators, int currentBotGeneratorProgress, string currentBotGeneratorType)
        {
            RemainingBotGenerators = remainingBotGenerators;
            CurrentBotGeneratorProgress = currentBotGeneratorProgress;
            CurrentBotGeneratorType = currentBotGeneratorType;
            IsValid = true;
        }
    }

    internal static class QuestingBotsInterop
    {
        private static bool _QuestingBotsLoadedChecked = false;
        private static bool _QuestingBotsInteropInited = false;

        private static bool _IsQuestingBotsLoaded;
        private static Type _QuestingBotsExternalType;

        private static MethodInfo _GetRemainingBotGeneratorsMethod;
        private static MethodInfo _GetCurrentBotGeneratorProgressMethod;
        private static MethodInfo _GetCurrentBotGeneratorTypeMethod;

        /**
         * Return true if Questing Bots is loaded in the client
         */
        public static bool IsQuestingBotsLoaded()
        {
            // Only check for Questing Bots once
            if (!_QuestingBotsLoadedChecked)
            {
                _QuestingBotsLoadedChecked = true;
                _IsQuestingBotsLoaded = Chainloader.PluginInfos.ContainsKey("com.DanW.QuestingBots");
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

                _QuestingBotsExternalType = Type.GetType("SPTQuestingBots.QuestingBotsExternal, SPTQuestingBots");

                // Only try to get the methods if we have the type
                if (_QuestingBotsExternalType != null)
                {
                    _GetRemainingBotGeneratorsMethod = AccessTools.Method(_QuestingBotsExternalType, "GetRemainingBotGenerators");
                    _GetCurrentBotGeneratorProgressMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentBotGeneratorProgress");
                    _GetCurrentBotGeneratorTypeMethod = AccessTools.Method(_QuestingBotsExternalType, "GetCurrentBotGeneratorType");
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
    }
}
