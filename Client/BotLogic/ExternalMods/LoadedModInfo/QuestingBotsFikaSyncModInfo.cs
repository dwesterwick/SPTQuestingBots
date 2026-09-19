using Comfort.Common;
using EFT;
using EFT.Communications;
using QuestingBots.BotLogic.ExternalMods.Functions.NetworkTransactions;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.ExternalMods.LoadedModInfo
{
    public class QuestingBotsFikaSyncModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = ModInfo.GUID + "fikasync";

        private System.Version? _currentBuildVersion;
        public System.Version CurrentBuildVersion
        {
            get
            {
                if (_currentBuildVersion == null)
                {
                    _currentBuildVersion = new System.Version(ModInfo.MOD_VERSION);
                }

                return _currentBuildVersion;
            }
        }

        public override System.Version MinCompatibleVersion => CurrentBuildVersion.MinBuild();
        public override System.Version MaxCompatibleVersion => CurrentBuildVersion.MaxBuild();

        public string Name => ModInfo.MODNAME + "FikaSync";

        public override string IncompatibilityMessage => $"Current version {PluginInfo.Metadata.Version} of {Name} is not compatible with Questing Bots. Please install a version between {MinCompatibleVersion} and {MaxCompatibleVersion} or spawning items in bot inventories with Fika clients may not work correctly.";

        public override bool IsCompatible()
        {
            if (base.IsCompatible())
            {
                return true;
            }

            NotificationManager.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            return false;
        }

        public override bool CheckInteropAvailability() => true;

        public override AbstractRunNetworkTransactionsFunction CreateRunNetworkTransactionsFunction(BotOwner _botOwner)
        {
            if (IsInstalled && IsCompatible())
            {
                return new FikaRunNetworkTransactionsFunction(_botOwner);
            }

            return base.CreateRunNetworkTransactionsFunction(_botOwner);
        }
    }
}
