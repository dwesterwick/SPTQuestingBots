using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal class BotDebugQuestingInfoBroadcaster : AbstractBotDataBroadcaster<BotDebugQuestingInfoPacket>
    {
        private const float BROADCAST_RATE = 10;

        public BotDebugQuestingInfoBroadcaster() : base(BROADCAST_RATE)
        {

        }

        protected override BotDebugQuestingInfoPacket CreatePacket(FikaBot bot) => BotDebugQuestingInfoPacket.Create(bot);
    }
}
