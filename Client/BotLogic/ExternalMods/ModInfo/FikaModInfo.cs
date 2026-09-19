using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib;
using Fika.Core.Networking.Packets.Player;
using QuestingBots.Utils;
using Version = System.Version;

namespace QuestingBots.BotLogic.ExternalMods.ModInfo
{
    public class FikaModInfo : AbstractExternalModInfo
    {
        public override string GUID { get; } = "com.fika.core";

        public override Version MinCompatibleVersion => new Version("2.4.0");
        public override Version MaxCompatibleVersion => new Version("2.99.99");
        private static Version _minInteropVersion => new Version("2.4.3");

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

            NotificationManager.DisplayWarningNotification(IncompatibilityMessage, ENotificationDurationType.Infinite);
            Singleton<LoggingUtil>.Instance.LogErrorToServerConsole(IncompatibilityMessage);
            return false;
        }

        public override bool CheckInteropAvailability()
        {
            if (PluginInfo.Metadata.Version >= _minInteropVersion)
            {
                CanUseInterop = true;
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogWarning(
                    $"Interop with Fika requires minimum version {_minInteropVersion}. Cannot add keys that unlock doors to other clients."
                );
            }

            return CanUseInterop;
        }

        public void TrySendItemAddedPacket(Player player, Item item)
        {
            if (!CanUseInterop)
            {
                return;
            }

            SendItemAddedPacket(player, item);
        }

        private static void SendItemAddedPacket(Player player, Item item)
        {
            if (player is FikaBot fikaBot)
            {
                var packet = new SpawnItemInInventoryPacket
                {
                    NetId = fikaBot.NetId,
                    ItemId = item.Id,
                    TemplateId = item.TemplateId,
                    Amount = item.StackObjectsCount,
                    ItemAddress = item.Parent,
                };
                Singleton<IFikaNetworkManager>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
            }
        }
    }
}
