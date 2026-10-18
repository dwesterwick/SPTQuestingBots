using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IBotDebugInfoSource : IBotDebugStaticInfo, IBotDebugCurrentInfo, IBotDebugQuestingInfo
    {
        public bool HasCurrentData { get; }
        public bool HasQuestingData { get; }
    }
}
