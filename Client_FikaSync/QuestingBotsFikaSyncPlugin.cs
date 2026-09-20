using BepInEx;
using BepInEx.Logging;
using EFT;
using EFT.InventoryLogic;
using Fika.Core.Main.Players;
using Fika.Core.Networking.Packets.Player;
using QuestingBots.ExternalMods.Functions.Multiplayer;
using QuestingBots.ExternalMods.Functions.NetworkTransactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots
{
    [BepInDependency("com.fika.core", "2.4.3")]
    [BepInDependency(ModInfo.GUID, "1.1.0")]
    [BepInPlugin(ModInfo.GUID + "fikasync", ModInfo.MODNAME + "FikaSync", ModInfo.MOD_VERSION)]
    internal class QuestingBotsFikaSyncPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource PluginLogger = null!;

        protected void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"{nameof(QuestingBotsFikaSyncPlugin)} has been loaded.");

            FikaMultiplayerFunctions.SetGetPayersFunc(FikaHelpers.GetCoopPlayers);
            FikaRunNetworkTransactionFunctions.SetMoveItemFunc(SendSpawnItemInInventoryPacket);
        }

        private bool SendSpawnItemInInventoryPacket(Player player, Item item)
        {
            FikaBot? fikaBot = player as FikaBot;
            if (fikaBot == null)
            {
                PluginLogger.LogError(player.name + " is not a FikaBot. Cannot send SpawnItemInInventoryPacket.");
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
            PluginLogger.LogInfo("Moving " + item.LocalizedName() + " to inventory of " + player.name + "...");
#endif

            return packet.TrySendToAllClients();
        }
    }
}
