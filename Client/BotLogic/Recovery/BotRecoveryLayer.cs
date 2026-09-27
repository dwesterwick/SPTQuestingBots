using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Recovery
{
    internal class BotRecoveryLayer : CustomLayerForQuesting
    {
        public BotRecoveryLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 25)
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
                //return updatePreviousState(false);
            }

            if (!ObjectiveManager.CoverPointSelector.HasCoverPoint)
            {
                if (previousState)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " no longer has a nearby cover point");
                }

                return updatePreviousState(false);
            }

            setNextAction(BotActionType.Recover, "Recover");
            return updatePreviousState(true);
        }
    }
}
