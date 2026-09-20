using Comfort.Common;
using EFT.Communications;
using QuestingBots.BotLogic.ExternalMods.Functions.Multiplayer;
using QuestingBots.Utils;
using System;

namespace QuestingBots.BotLogic.ExternalMods.LoadedModInfo
{
    public class FikaModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.core";

        public override Version MinCompatibleVersion => new Version("2.4.3");
        public override Version MaxCompatibleVersion => new Version("2.99.99");

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
