using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Components
{
    internal class BotInfoDataBroadcaster : MonoBehaviourDelayedUpdate
    {
        private List<BotOwner> initializedBots = new List<BotOwner>();

        public BotInfoDataBroadcaster()
        {
            UpdateInterval = QuestingBotsFikaSyncPlugin.BROADCAST_RATE;
        }

        protected void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            if (!Singleton<IBotGame>.Instantiated)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("No IBotGame instance available");
                return;
            }

            foreach (BotOwner bot in Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners)
            {
                SendBotStaticInfoDataPacket(bot);
                SendBotCurrentInfoDataPacket(bot);
            }
        }

        private void SendBotStaticInfoDataPacket(BotOwner bot)
        {
            // Don't waste bandwidth on data that doesn't change
            if (initializedBots.Contains(bot))
            {
                return;
            }

            BotDebugStaticInfoPacket packet = BotDebugStaticInfoPacket.Create(bot);
            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send BotDebugStaticInfoPacket for " + bot.name);
                return;
            }

            if (bot.GetObjectiveManager()?.IsInitialized == true)
            {
#if DEBUG
                QuestingBotsFikaSyncPlugin.PluginLogger.LogDebug(bot.name + " is initialized");
#endif
                initializedBots.Add(bot);
            }
        }

        private void SendBotCurrentInfoDataPacket(BotOwner bot)
        {
            BotDebugCurrentInfoPacket packet = BotDebugCurrentInfoPacket.Create(bot);
            if (!packet.TrySendToAllClients())
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("Could not send BotDebugCurrentInfoPacket for " + bot.name);
            }
        }
    }
}
