using EFT;
using QuestingBots.BehaviorExtensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Recovery
{
    internal class BotRecoveryLayer : CustomLayerForQuesting
    {
        public BotRecoveryLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 4)
        {

        }

        public override string GetName()
        {
            return "BotRecoveryLayer";
        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        public override bool IsActive()
        {
            if (!canUpdate())
            {
                return previousState;
            }

            if (ObjectiveManager.PatrolPointSelector.HasPatrolPoint && !ObjectiveManager.PatrolPointSelector.IsWayReserved)
            {
                return updatePreviousState(false);
            }

            ObjectiveManager.CoverPointSelector.RefreshCoverPoint();
            if (ObjectiveManager.CoverPointSelector.CoverPoint == null)
            {
                return updatePreviousState(false);
            }

            setNextAction(BotActionType.Recover, "Recover");
            return updatePreviousState(true);
        }
    }
}
