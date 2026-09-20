using Comfort.Common;
using EFT.Communications;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.ExternalMods.LoadedModInfo
{
    public class QuestingBotsFikaSyncModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = ModInfo.GUID + "fikasync";

        public override System.Version MinCompatibleVersion => new System.Version(1, 0, 0);
        public override System.Version MaxCompatibleVersion => new System.Version(1, 99, 99);

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
    }
}
