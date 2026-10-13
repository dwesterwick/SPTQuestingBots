using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.PacketHandlers
{
    internal abstract class AbstractPacketHandler
    {
        private static List<AbstractPacketHandler> _packetHandlers = new List<AbstractPacketHandler>();

        internal abstract void Setup();
        internal abstract void RegisterPackets(FikaClient client);

        public static void SetupAllPacketHandlers()
        {
            IEnumerable<AbstractPacketHandler> packetHandlers = GetAllPacketHandlerTypes()
                .Select(t => (AbstractPacketHandler)Activator.CreateInstance(t));

            foreach (AbstractPacketHandler packetHandler in packetHandlers)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogInfo($"Setting up packet handler {packetHandler.GetType().Name}...");

                _packetHandlers.Add(packetHandler);
                packetHandler.Setup();
            }

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        }

        public static IEnumerable<Type> GetAllPacketHandlerTypes()
        {
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(AbstractPacketHandler)));
        }

        public static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent eventInfo)
        {
            FikaClient? client = eventInfo.Manager as FikaClient;
            if (client == null)
            {
                return;
            }

            foreach (AbstractPacketHandler packetHandler in _packetHandlers)
            {
                packetHandler.RegisterPackets(client);
            }
        }
    }
}
