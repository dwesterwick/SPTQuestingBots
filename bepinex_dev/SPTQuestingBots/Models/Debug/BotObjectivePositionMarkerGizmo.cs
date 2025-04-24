using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Components;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Models.Debug
{
    public class BotObjectivePositionMarkerGizmo : AbstractBotPathMarkerGizmo
    {
        public BotObjectivePositionMarkerGizmo(BotOwner _bot, float _markerRadius) : base(_bot, _markerRadius, "Objective Position", Color.green)
        {
            
        }

        protected override bool IsEnabled() => QuestingBotsPluginConfig.BotPathOverlayTypes.Value.HasFlag(BotPathOverlayType.QuestTarget);
        protected override bool HasValidPath() => (BotObjectiveManager?.BotPath?.HasPath == true) && BotObjectiveManager.IsQuestingAllowed;
        protected override Vector3 GetPosition() => BotObjectiveManager.BotPath.TargetPosition;
        protected override NavMeshPathStatus? GetPathStatus() => BotObjectiveManager.BotPath.Status;
    }
}
