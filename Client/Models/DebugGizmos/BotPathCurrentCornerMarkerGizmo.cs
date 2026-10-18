using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using QuestingBots.Models.DebugGizmos.DebugInfo;

namespace QuestingBots.Models.DebugGizmos
{
    public class BotPathCurrentCornerMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotPathCurrentCornerMarkerGizmo(IBotDebugInfoSource debugInfoSource, float _markerRadius) : base(debugInfoSource, _markerRadius, "Current Corner", Color.yellow)
        {

        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTCurrentCorner);

        protected override bool HasValidPath()
        {
            if (!DebugInfoSource.HasEftPath)
            {
                return false;
            }

            // If the current corner is the same as the target point, we don't need to show the marker
            if (QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTTarget))
            {
                if (GetPosition() == DebugInfoSource.CurrentEftPathTarget)
                {
                    return false;
                }
            }

            return true;
        }

        protected override Vector3? GetPosition() => DebugInfoSource.CurrentEftPathCorner;
        protected override NavMeshPathStatus? GetPathStatus() => null;
    }
}
