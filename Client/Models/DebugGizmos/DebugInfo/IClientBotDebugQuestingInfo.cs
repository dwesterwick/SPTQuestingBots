using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Models.Questing;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IClientBotDebugQuestingInfo : IClientBotDebugInfo
    {
        public bool AllowedToQuest { get; }
        public bool IsQuesting { get; }
        public string QuestName { get; }
        public string QuestObjectiveName { get; }
        public string QuestObjectiveStepName { get; }
        public EJobAssignmentStatus QuestStatus { get; }
        public EBotQuestingDecision CurrentDecision { get; }
        public UnityEngine.AI.NavMeshPathStatus PathStatus { get; }
    }
}
