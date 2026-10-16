using QuestingBots.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IClientBotDebugStaticInfo : IClientBotDebugInfo
    {
        public int BotId { get; }
        public bool IsInitialized { get; }
        public EBotType BotType { get; }
        public string BaseBrainName { get; }
    }
}
