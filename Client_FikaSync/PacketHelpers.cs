using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using Fika.Core.Main.Utils;
using Fika.Core.Networking;
using Fika.Core.Networking.LiteNetLib.Utils;

namespace QuestingBots
{
    internal static class PacketHelpers
    {
        internal static void SendToAllClients<T>(this T packet) where T : INetSerializable
        {
            if (FikaBackendUtils.IsClient) // safeguard
            {
                return;
            }

            if (Singleton<IFikaNetworkManager>.Instance is FikaServer server)
            {
                server.SendData(ref packet, Fika.Core.Networking.LiteNetLib.DeliveryMethod.ReliableOrdered);

                return;
            }

            QuestingBotsFikaSyncPlugin.PluginLogger.LogError($"NetworkManager was not a server when trying to send {packet.GetType().Name}");
        }
    }
}
