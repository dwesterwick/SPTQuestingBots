using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IClientBotDebugStaticInfo : IClientBotDebugInfo
    {
        public bool IsInitialized { get; }
        public EBotType BotType { get; }
        public string CorrectedNickname { get; }
        public int Level { get; }
    }
}
