using EFT;
using Fika.Core.Main.Players;
using Fika.Core.Networking.LiteNetLib.Utils;
using QuestingBots.Helpers;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal abstract class AbstractBotDataBroadcaster<T> : IAbstractBotDataBroadcaster where T : INetSerializable, IClientBotDebugInfo
    {
        protected float BroadcastRate { get; private set; }

        private Stopwatch broadcastTimer = Stopwatch.StartNew();

        protected bool CanBroadcast => broadcastTimer.ElapsedMilliseconds >= 1000f / BroadcastRate;

        public AbstractBotDataBroadcaster(float broadcastRate)
        {
            BroadcastRate = broadcastRate;
        }

        public void Broadcast(IEnumerable<Player> botPlayers)
        {
            if (!CanBroadcast)
            {
                return;
            }

            foreach (Player botPlayer in botPlayers)
            {
                FikaBot? bot = botPlayer as FikaBot;
                if (bot == null)
                {
                    QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Player " + botPlayer.name + " is not a FikaBot");
                    continue;
                }

                SendBotDataPacket(bot);
            }

            broadcastTimer.Restart();
        }

        protected abstract T CreatePacket(FikaBot bot);
        private void SendBotDataPacket(FikaBot bot)
        {
            T packet = CreatePacket(bot);
            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send " + typeof(T).Name + " for " + bot.name);
                return;
            }
        }
    }
}
