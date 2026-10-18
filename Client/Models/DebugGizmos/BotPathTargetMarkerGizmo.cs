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
    public class BotPathTargetMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotPathTargetMarkerGizmo(IBotDebugInfoSource debugInfoSource, float _markerRadius) : base(debugInfoSource, _markerRadius, "Target Position", Color.cyan)
        {
            
        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTTarget);

        protected override bool HasValidPath()
        {
            if (!DebugInfoSource.HasEftPath)
            {
                return false;
            }

            if (DebugInfoSource.CurrentEftPathTarget == Vector3.negativeInfinity)
            {
                return false;
            }

            if (QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.QuestTarget))
            {
                if (DebugInfoSource.HasQuestingPath && DebugInfoSource.AllowedToQuest)
                {
                    Vector3? position = GetPosition();
                    if (!position.HasValue)
                    {
                        return true;
                    }

                    // If the EFT path target position is basically the same as the bot's quest target position, we don't need to show the marker
                    if (Vector3.Distance(position.Value, DebugInfoSource.CurrentQuestingPathTarget) < MarkerRadius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override Vector3? GetPosition() => DebugInfoSource.CurrentEftPathTarget;
        protected override NavMeshPathStatus? GetPathStatus() => null;
    }
}
