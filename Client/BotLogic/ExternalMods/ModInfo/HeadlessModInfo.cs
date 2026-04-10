using System;
using System.Reflection;
using Comfort.Common;
using EFT.Game.Spawning;
using HarmonyLib;
using QuestingBots.Utils;
using UnityEngine;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class HeadlessModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.headless";

        public override Version MinCompatibleVersion => new Version("1.4.0");
        public override Version MaxCompatibleVersion => new Version("1.4.99");

        public override string IncompatibilityMessage => $"Installed Fika Headless ({PluginInfo.Metadata.Version}) is not compatible with Questing Bots spawning system. Please upgrade Fika Headless to {MinCompatibleVersion} or newer to use the QB spawning system.";

        public MethodInfo RunMemoryCleanupMethod = null!;

        public override bool IsCompatible()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled) return true;

            if (base.IsCompatible())
            {
                Type headlessGameType = AccessTools.TypeByName("Fika.Headless.Classes.GameMode.HeadlessGame");
                if (headlessGameType != null)
                {
                    RunMemoryCleanupMethod = AccessTools.Method(headlessGameType, "RunMemoryCleanup");
                    if (RunMemoryCleanupMethod != null)
                    {
                        new Patches.Spawning.HeadlessGameStartPatch().Enable();

                        return true;
                    }
                }
            }

            NotificationManagerClass.DisplayWarningNotification(IncompatibilityMessage, EFT.Communications.ENotificationDurationType.Long);
            Singleton<LoggingUtil>.Instance.LogWarningToServerConsole(IncompatibilityMessage);
            // TODO: Decide if to disable BotSpawns config and spawn patches. Spawns will work, however the game start will not be delayed.
            // Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled = false;
            // DisableSpawnPatches();
            return false;
        }

        public override bool CheckInteropAvailability() => true;

        public static Vector3? TryGetHeadlessSpawnPoint()
        {
            Type? fikaGameType = AccessTools.TypeByName("Fika.Core.Main.GameMode.IFikaGame");
            if (fikaGameType == null) return null;

            Type fikaGameSingletonType = typeof(Singleton<>).MakeGenericType(fikaGameType);
            PropertyInfo? instanceProperty = AccessTools.Property(fikaGameSingletonType, "Instance");
            object? fikaGame = instanceProperty?.GetValue(null);
            if (fikaGame == null) return null;

            PropertyInfo? baseGameControllerProperty = AccessTools.Property(fikaGameType, "GameController");
            object? baseGameController = baseGameControllerProperty?.GetValue(fikaGame);

            Type? baseGameControllerType = AccessTools.TypeByName("Fika.Core.Main.GameMode.BaseGameController");
            PropertyInfo? spawnPointProperty = AccessTools.Property(baseGameControllerType, "SpawnPoint");
            ISpawnPoint? spawnPoint = spawnPointProperty?.GetValue(baseGameController) as ISpawnPoint;

            return spawnPoint?.Position;
        }
    }
}
