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
        internal static bool TrySendToAllClients<T>(this T packet) where T : INetSerializable
        {
            if (FikaBackendUtils.IsClient) // safeguard
            {
#if DEBUG
                QuestingBotsFikaSyncPlugin.PluginLogger.LogDebug($"Cannot send {packet.GetType().Name} from a client");
#endif

                return false;
            }

            FikaServer? server = Singleton<IFikaNetworkManager>.Instance as FikaServer;
            if (server == null)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError($"NetworkManager was not a server when trying to send {packet.GetType().Name}");
                return false;
            }

            server.SendData(ref packet, Fika.Core.Networking.LiteNetLib.DeliveryMethod.ReliableOrdered);

            return true;
        }
    }
}
