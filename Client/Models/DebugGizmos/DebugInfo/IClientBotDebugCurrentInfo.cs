using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IClientBotDebugCurrentInfo : IClientBotDebugInfo
    {
        public bool IsDead { get; }
        public ClassVector3 Position { get; }
    }
}
