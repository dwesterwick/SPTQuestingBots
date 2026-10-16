using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal interface IAbstractBotDataBroadcaster
    {
        public void Broadcast(IEnumerable<Player> botPlayers);
    }
}
