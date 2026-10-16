using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using QuestingBots.BehaviorExtensions;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace QuestingBots.Components
{
    internal class BotInfoDataBroadcaster : MonoBehaviourDelayedUpdate
    {
        private const float BROADCAST_RATE_STATIC = 5;
        private const float BROADCAST_RATE_CURRENT = 20;

        private Stopwatch broadcastTimerStatic = Stopwatch.StartNew();
        private Stopwatch broadcastTimerCurrent = Stopwatch.StartNew();

        private bool CanBroadcastStaticData => broadcastTimerStatic.ElapsedMilliseconds >= 1000f / BROADCAST_RATE_STATIC;
        private bool CanBroadcastCurrentData => broadcastTimerCurrent.ElapsedMilliseconds >= 1000f / BROADCAST_RATE_CURRENT;

        public BotInfoDataBroadcaster()
        {
            UpdateInterval = 5;
        }

        protected void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            HostGameController? hostGameController = Singleton<IBotGame>.Instance as HostGameController;
            if (hostGameController == null)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("No HostGameController instance available");
                return;
            }

            BroadcastBotStaticInfoData(hostGameController);
            BroadcastBotCurrentInfoData(hostGameController);
        }

        private void BroadcastBotStaticInfoData(HostGameController hostGameController)
        {
            if (!CanBroadcastStaticData)
            {
                return;
            }

            foreach (Player botPlayer in hostGameController.Bots.Values)
            {
                FikaBot? bot = botPlayer as FikaBot;
                if (bot == null)
                {
                    QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Player " + botPlayer.name + " is not a FikaBot");
                    continue;
                }

                SendBotStaticInfoDataPacket(bot);
            }

            broadcastTimerStatic.Restart();
        }

        private void BroadcastBotCurrentInfoData(HostGameController hostGameController)
        {
            if (!CanBroadcastCurrentData)
            {
                return;
            }

            foreach (Player botPlayer in hostGameController.Bots.Values)
            {
                FikaBot? bot = botPlayer as FikaBot;
                if (bot == null)
                {
                    QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Player " + botPlayer.name + " is not a FikaBot");
                    continue;
                }

                SendBotCurrentInfoDataPacket(bot);
            }

            broadcastTimerCurrent.Restart();
        }

        private void SendBotStaticInfoDataPacket(FikaBot bot)
        {
            BotDebugStaticInfoPacket packet = BotDebugStaticInfoPacket.Create(bot);
            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send BotDebugStaticInfoPacket for " + bot.name);
                return;
            }
        }

        private void SendBotCurrentInfoDataPacket(FikaBot bot)
        {
            BotDebugCurrentInfoPacket packet = BotDebugCurrentInfoPacket.Create(bot);
            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send BotDebugCurrentInfoPacket for " + bot.name);
            }
        }
    }
}
