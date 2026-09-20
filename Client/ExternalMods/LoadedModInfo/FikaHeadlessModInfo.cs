using Comfort.Common;
using EFT.Communications;
using HarmonyLib;
using QuestingBots.Utils;
using System;
using System.Reflection;

namespace QuestingBots.ExternalMods.LoadedModInfo
{
    public class FikaHeadlessModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.headless";

        public override Version MinCompatibleVersion => new Version("1.5.0");
        public override Version MaxCompatibleVersion => new Version("1.99.99");

        public override string IncompatibilityMessage => $"Installed Fika Headless ({PluginInfo.Metadata.Version}) is not compatible with Questing Bots spawning system. Please upgrade Fika Headless to {MinCompatibleVersion} or newer to use the QB spawning system.";

        public MethodInfo RunMemoryCleanupMethod = null!;

        public override bool IsCompatible()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return true;
            }

            if (base.IsCompatible())
            {
                return TryPatchGameStart();
            }

            NotificationManager.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogWarningToServerConsole(IncompatibilityMessage);
            // TODO: Decide if to disable BotSpawns config and spawn patches. Spawns will work, however the game start will not be delayed.
            // Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled = false;
            // DisableSpawnPatches();
            return false;
        }

        public override bool CheckInteropAvailability() => true;

        private bool TryPatchGameStart()
        {
            Type headlessGameType = AccessTools.TypeByName("Fika.Headless.Classes.GameMode.HeadlessGame");
            if (headlessGameType == null)
            {
                return false;
            }

            RunMemoryCleanupMethod = AccessTools.Method(headlessGameType, "RunMemoryCleanup");
            if (RunMemoryCleanupMethod == null)
            {
                return false;
            }

            new Patches.Spawning.HeadlessGameStartPatch().Enable();

            return true;
        }
    }
}
