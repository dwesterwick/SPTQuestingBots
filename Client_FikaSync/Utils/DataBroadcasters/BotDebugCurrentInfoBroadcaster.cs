using Fika.Core.Main.Players;
using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal class BotDebugCurrentInfoBroadcaster : AbstractBotDataBroadcaster<BotDebugCurrentInfoPacket>
    {
        private const float BROADCAST_RATE = 20;

        public BotDebugCurrentInfoBroadcaster() : base(BROADCAST_RATE)
        {

        }

        protected override bool CanSendPacket(FikaBot bot) => bot.AIData.BotOwner.IsInitialized();

        protected override BotDebugCurrentInfoPacket CreatePacket(FikaBot bot) => BotDebugCurrentInfoPacket.Create(bot);
    }
}
