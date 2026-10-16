using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking;
using Fika.Core.Networking.Packets.Player;
using QuestingBots.ExternalMods.Functions.NetworkTransactions;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal class SpawnItemInInventoryHandler : AbstractPacketHandler
    {
        public SpawnItemInInventoryHandler() { }

        internal override void Setup()
        {
            FikaRunNetworkTransactionFunctions.SetMoveItemFunc(SendSpawnItemInInventoryPacket);
        }

        internal override void RegisterPackets(FikaClient client)
        {

        }

        private static bool SendSpawnItemInInventoryPacket(Player player, Item item)
        {
            FikaBot? fikaBot = player as FikaBot;
            if (fikaBot == null)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError(player.name + " is not a FikaBot. Cannot send SpawnItemInInventoryPacket.");
                return false;
            }

            SpawnItemInInventoryPacket packet = new SpawnItemInInventoryPacket()
            {
                NetId = fikaBot.NetId,
                ItemId = item.Id,
                TemplateId = item.TemplateId,
                Amount = item.StackObjectsCount,
                ItemAddress = item.Parent,
            };

#if DEBUG
            QuestingBotsFikaSyncPlugin.PluginLogger.LogInfo("Moving " + item.LocalizedName() + " to inventory of " + player.name + "...");
#endif

            return packet.TrySendToAllClients();
        }
    }
}
