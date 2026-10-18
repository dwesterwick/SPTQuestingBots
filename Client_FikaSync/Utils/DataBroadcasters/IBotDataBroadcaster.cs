using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Utils.DataBroadcasters
{
    internal interface IBotDataBroadcaster
    {
        public void Broadcast(IEnumerable<Player> botPlayers);
    }
}
