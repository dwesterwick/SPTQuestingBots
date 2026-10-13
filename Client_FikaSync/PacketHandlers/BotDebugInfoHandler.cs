using Fika.Core.Networking;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal class BotDebugInfoHandler : AbstractPacketHandler
    {
        internal BotDebugInfoHandler() { }

        internal override void Setup()
        {
            new Patches.CreateBotInfoBroadcasterPatch().Enable();
        }

        internal override void RegisterPackets(FikaClient client)
        {
            client.RegisterPacket<BotDebugStaticInfoPacket>(OnBotDebugStaticInfoPacket);
            client.RegisterPacket<BotDebugCurrentInfoPacket>(OnBotDebugCurrentInfoPacket);
        }

        internal static void OnBotDebugCurrentInfoPacket(BotDebugCurrentInfoPacket packet)
        {
            if (!FikaHelpers.TryGetDebugData(out Components.DebugData? debugData) || (debugData == null))
            {
                return;
            }

            debugData.UpdateClientBotCurrentInfo(packet);
        }

        internal static void OnBotDebugStaticInfoPacket(BotDebugStaticInfoPacket packet)
        {
            if (!FikaHelpers.TryGetDebugData(out Components.DebugData? debugData) || (debugData == null))
            {
                return;
            }

            debugData.UpdateClientBotStaticInfo(packet);
        }
    }
}
