using Comfort.Common;
using EFT.Communications;
using QuestingBots.Utils;
using System;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class FikaModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.core";

        public override Version MinCompatibleVersion => new Version("2.4.0");
        public override Version MaxCompatibleVersion => new Version("2.99.99");

        public override string IncompatibilityMessage => $"Installed Fika ({PluginInfo.Metadata.Version}) is not compatible with Questing Bots spawning system. Please upgrade Fika to {MinCompatibleVersion} or newer to use the QB spawning system.";

        public override bool IsCompatible()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return true;
            }

            if (base.IsCompatible())
            {
                return true;
            }

            NotificationManager.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Infinite);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            // TODO: Disable BotSpawns config and disable spawn patches. Raid may not start.
            // Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled = false;
            // DisableSpawnPatches();
            return false;
        }

        public override bool CheckInteropAvailability() => true;
    }
}
