using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal class BotDebugStaticInfoBroadcaster : AbstractBotDataBroadcaster<BotDebugStaticInfoPacket>
    {
        private const float BROADCAST_RATE = 5;

        public BotDebugStaticInfoBroadcaster() : base(BROADCAST_RATE)
        {

        }

        protected override bool CanSendPacket(FikaBot bot) => bot.AIData.BotOwner != null;

        protected override BotDebugStaticInfoPacket CreatePacket(FikaBot bot) => BotDebugStaticInfoPacket.Create(bot);
    }
}
