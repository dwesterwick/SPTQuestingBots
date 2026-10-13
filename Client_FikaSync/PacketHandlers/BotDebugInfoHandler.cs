using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal static class BotDebugInfoHandler
    {
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
