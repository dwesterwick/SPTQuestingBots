using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal static class SpawnItemInInventoryHandler
    {
        internal static bool SendSpawnItemInInventoryPacket(Player player, Item item)
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
