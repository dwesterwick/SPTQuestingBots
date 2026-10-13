using BepInEx;
using BepInEx.Logging;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using QuestingBots.ExternalMods.Functions.Multiplayer;
using QuestingBots.ExternalMods.Functions.NetworkTransactions;
using QuestingBots.Helpers;
using QuestingBots.Patches.Spawning;
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
        public const int BROADCAST_RATE = 20;

        internal static ManualLogSource PluginLogger = null!;

        protected void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"{nameof(QuestingBotsFikaSyncPlugin)} has been loaded.");

            FikaMultiplayerFunctions.SetGetPayersFunc(FikaHelpers.GetCoopPlayers);
            FikaRunNetworkTransactionFunctions.SetMoveItemFunc(PacketHandlers.SpawnItemInInventoryHandler.SendSpawnItemInInventoryPacket);

            GameStartPatch.OnWaitForBotGeneratorsProgressChanged += PacketHandlers.BotGeneratorProgressHandler.OnWaitForBotGeneratorsProgressChanged;

            new Patches.CreateBotInfoBroadcasterPatch().Enable();

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        private void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent eventInfo)
        {
            if (eventInfo.Manager is FikaClient client)
            {
                client.RegisterPacket<BotDebugStaticInfoPacket>(PacketHandlers.BotDebugInfoHandler.OnBotDebugStaticInfoPacket);
                client.RegisterPacket<BotDebugCurrentInfoPacket>(PacketHandlers.BotDebugInfoHandler.OnBotDebugCurrentInfoPacket);
                client.RegisterPacket<BotGeneratorProgressPacket>(PacketHandlers.BotGeneratorProgressHandler.OnBotGeneratorProgressPacketReceived);
            }
        }
    }
}
