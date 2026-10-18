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
    internal abstract class AbstractBotDataBroadcaster<T> : IBotDataBroadcaster where T : INetSerializable, IBotDebugInfo
    {
        protected float BroadcastRate { get; private set; }

        private Stopwatch broadcastTimer = Stopwatch.StartNew();

        protected bool CanBroadcastByTime => broadcastTimer.ElapsedMilliseconds >= 1000f / BroadcastRate;

        public AbstractBotDataBroadcaster(float broadcastRate)
        {
            BroadcastRate = broadcastRate;
        }

        protected abstract bool CanSendPacket(FikaBot bot);

        public void Broadcast(IEnumerable<Player> botPlayers)
        {
            if (!CanBroadcastByTime)
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

                if (!CanSendPacket(bot))
                {
                    return;
                }

                try
                {
                    SendBotDataPacket(bot);
                }
                catch (Exception ex)
                {
                    QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Exception when sending packet " + typeof(T).Name + " for bot with NetId=" + bot.NetId + ": " + ex.Message);
                }
            }

            broadcastTimer.Restart();
        }

        protected abstract T CreatePacket(FikaBot bot);
        private void SendBotDataPacket(FikaBot bot)
        {
            T packet = CreatePacket(bot);
            if (!packet.TrySendToAllClients())
            {
                throw new InvalidOperationException("Could not send " + typeof(T).Name + " for bot with NetId=" + bot.NetId);
            }
        }
    }
}
