using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine.AI;
using UnityEngine;

namespace SPTQuestingBots.Models.Debug
{
    public class BotPathCurrentCornerMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotPathCurrentCornerMarkerGizmo(BotOwner _bot, float _markerRadius) : base(_bot, _markerRadius, "Current Corner", Color.yellow)
        {

        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTCurrentCorner);

        protected override bool HasValidPath()
        {
            if (BotOwner?.Mover?._pathController?.HavePath != true)
            {
                return false;
            }

            // If the current corner is the same as the target point, we don't need to show the marker
            if (QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.EFTTarget))
            {
                if (GetPosition() == BotOwner.Mover._pathController.TargetPoint)
                {
                    return false;
                }
            }

            return true;
        }

        protected override Vector3 GetPosition() => BotOwner.Mover._pathController.CurrentCorner();
        protected override NavMeshPathStatus? GetPathStatus() => null;
    }
}
