using Fika.Core.Main.Utils;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal class BotDebugInfoHandler : AbstractPacketHandler
    {
        public BotDebugInfoHandler() { }

        internal override void Setup()
        {
            if (!BotDebugInfoHelpers.CanEnableBotDebugDataBroadcasting())
            {
                return;
            }

            QuestingBotsFikaSyncPlugin.PluginLogger.LogInfo("Enabling bot debug data broadcasting...");

            new Patches.CreateBotInfoBroadcasterPatch().Enable();

            //if (!FikaBackendUtils.IsHeadless)
            {
                FikaEventDispatcher.SubscribeEvent<GameWorldStartedEvent>(OnGameWorldStartedEvent);
                FikaEventDispatcher.SubscribeEvent<FikaGameEndedEvent>(OnFikaGameEndedEvent);
            }
        }

        internal override void RegisterPackets(FikaClient client)
        {
            client.RegisterPacket<BotDebugStaticInfoPacket>(OnBotDebugStaticInfoPacket);
            client.RegisterPacket<BotDebugCurrentInfoPacket>(OnBotDebugCurrentInfoPacket);
            client.RegisterPacket<BotDebugQuestingInfoPacket>(OnBotDebugQuestingInfoPacket);
        }

        private static void OnGameWorldStartedEvent(GameWorldStartedEvent eventInfo)
        {
#if DEBUG
            QuestingBotsFikaSyncPlugin.PluginLogger.LogDebug("Adding GameWorld components...");
#endif

            Patches.BotsControllerInitPatch.GetOrAddCommonGameWorldComponents();
        }

        private static void OnFikaGameEndedEvent(FikaGameEndedEvent eventInfo)
        {
#if DEBUG
            QuestingBotsFikaSyncPlugin.PluginLogger.LogDebug("Removing GameWorld components...");
#endif

            Patches.BotsControllerStopPatch.RemoveCommonGameWorldComponents();
        }

        private static void OnBotDebugStaticInfoPacket(BotDebugStaticInfoPacket packet)
        {
            if (!FikaHelpers.TryGetDebugData(out Components.DebugData? debugData) || (debugData == null))
            {
                return;
            }

            if (!debugData.TryUpdateClientBotStaticInfo(packet))
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not update static debug info for bot with NetId=" + packet.NetId);
            }
        }

        private static void OnBotDebugCurrentInfoPacket(BotDebugCurrentInfoPacket packet)
        {
            if (!FikaHelpers.TryGetDebugData(out Components.DebugData? debugData) || (debugData == null))
            {
                return;
            }

            debugData.UpdateClientBotCurrentInfo(packet);
        }

        private static void OnBotDebugQuestingInfoPacket(BotDebugQuestingInfoPacket packet)
        {
            if (!FikaHelpers.TryGetDebugData(out Components.DebugData? debugData) || (debugData == null))
            {
                return;
            }

            debugData.UpdateClientBotQuestingInfo(packet);
        }
    }
}
