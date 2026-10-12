using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots
{
    internal class BotInfoDataBroadcaster : MonoBehaviourDelayedUpdate
    {
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
                SendBotInfoDataPacket(bot);
            }
        }

        private void SendBotInfoDataPacket(BotOwner bot)
        {
            BotDebugInfoPacket packet = new BotDebugInfoPacket()
            {
                BotId = bot.Id,
                IsDead = bot.IsDead,
                Position = bot.Position,
            };

            packet.TrySendToAllClients();
        }
    }
}
