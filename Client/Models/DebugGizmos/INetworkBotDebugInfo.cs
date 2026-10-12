using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos
{
    public interface INetworkBotDebugInfo
    {
        public int BotId { get; }
        public bool IsDead { get; }
        public ClassVector3 Position { get; }
    }
}
