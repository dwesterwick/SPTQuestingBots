using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine.AI;
using UnityEngine;

namespace QuestingBots.Models.DebugGizmos
{
    public class BotPathTargetMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotPathTargetMarkerGizmo(BotOwner _bot, float _markerRadius) : base(_bot, _markerRadius, "Target Position", Color.cyan)
        {
            
        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTTarget);

        protected override bool HasValidPath()
        {
            if (BotOwner?.Mover?.ActualPathController?.HavePath != true)
            {
                return false;
            }

            if (!BotOwner.Mover.ActualPathController.TargetPoint.HasValue)
            {
                return false;
            }

            if (QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.QuestTarget))
            {
                if ((BotObjectiveManager?.BotPath?.HasPath == true) && BotObjectiveManager.IsQuestingAllowed)
                {
                    Vector3? position = GetPosition();
                    if (!position.HasValue)
                    {
                        return true;
                    }

                    // If the EFT path target position is basically the same as the bot's quest target position, we don't need to show the marker
                    if (Vector3.Distance(position.Value, BotObjectiveManager.BotPath.TargetPosition) < MarkerRadius)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        protected override Vector3? GetPosition() => BotOwner.Mover.ActualPathController.TargetPoint;
        protected override NavMeshPathStatus? GetPathStatus() => null;
    }
}
