using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal static class BotDebugInfoHandler
    {
        internal static void OnBotDebugInfoPacket(BotDebugInfoPacket packet)
        {
            if (!FikaHelpers.TryGetGameWorld(out GameWorld? gameWorld) || (gameWorld == null))
            {
                return;
            }

            if (!Singleton<GameWorld>.Instance.TryGetComponent(out Components.DebugData debugData))
            {
                return;
            }

            debugData.UpdateNetworkBotInfo(packet.BotId, packet);
        }
    }
}
