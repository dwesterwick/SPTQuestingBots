using Comfort.Common;
using EFT;
using EFT.Communications;
using QuestingBots.ExternalMods.Functions.Multiplayer;
using QuestingBots.ExternalMods.Functions.NetworkTransactions;
using QuestingBots.Utils;
using System;

namespace QuestingBots.ExternalMods.LoadedModInfo
{
    public class FikaModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.core";

        public override System.Version MinCompatibleVersion => new System.Version("2.4.3");
        public override System.Version MaxCompatibleVersion => new System.Version("2.99.99");

        public override string IncompatibilityMessage => $"Installed Fika ({PluginInfo.Metadata.Version}) is not compatible with Questing Bots. Please upgrade Fika to {MinCompatibleVersion} or newer.";

        private bool _isCompatible = false;
        public override bool IsCompatible()
        {
            if (base.IsCompatible())
            {
                _isCompatible = true;
                return true;
            }

            NotificationManager.DisplayWarningNotification(IncompatibilityMessage, ENotificationDurationType.Infinite);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            return false;
        }

        public override bool CheckInteropAvailability() => true;

        public override AbstractRunNetworkTransactionFunctions CreateRunNetworkTransactionFunctions(BotOwner _botOwner)
        {
            if (_isCompatible)
            {
                return new FikaRunNetworkTransactionFunctions(_botOwner);
            }

            return base.CreateRunNetworkTransactionFunctions(_botOwner);
        }

        public override AbstractMultiplayerFunctions CreateMultiplayerFunctions()
        {
            if (_isCompatible)
            {
                return new FikaMultiplayerFunctions();
            }

            return base.CreateMultiplayerFunctions();
        }
    }
}
