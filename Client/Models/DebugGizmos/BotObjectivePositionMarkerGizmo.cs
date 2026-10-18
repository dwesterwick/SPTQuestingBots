using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Models.DebugGizmos
{
    public class BotObjectivePositionMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotObjectivePositionMarkerGizmo(IBotDebugInfoSource debugInfoSource, float _markerRadius) : base(debugInfoSource, _markerRadius, "Objective Position", Color.green)
        {
            
        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.QuestTarget);
        protected override bool HasValidPath() => DebugInfoSource.AllowedToQuest && DebugInfoSource.HasQuestingPath;
        protected override Vector3? GetPosition() => DebugInfoSource.CurrentQuestingPathTarget;
        protected override NavMeshPathStatus? GetPathStatus() => DebugInfoSource.PathStatus;
    }
}
