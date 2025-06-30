using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.BotLogic.BotMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class BotFollowerRegroupLayer : CustomLayerForQuesting
    {
        public BotFollowerRegroupLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 250)
        {

        }

        public override string GetName()
        {
            return "BotFollowerRegroupLayer";
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

            BotQuestingDecisionMonitor decisionMonitor = objectiveManager.BotMonitor.GetMonitor<BotQuestingDecisionMonitor>();
            if (decisionMonitor.CurrentDecision != BotQuestingDecision.HelpBoss)
            {
                return updatePreviousState(false);
            }

            setNextAction(BotActionType.FollowerRegroup, "RegroupWithBoss");
            return updatePreviousState(true);
        }
    }
}
