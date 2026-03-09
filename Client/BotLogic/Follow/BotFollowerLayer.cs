using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.Follow
{
    internal class BotFollowerLayer : CustomLayerForQuesting
    {
        private double maxDistanceFromBoss = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeQuesting.Min;

        public BotFollowerLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 25)
        {
            
        }

        public override string GetName()
        {
            return "BotFollowerLayer";
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
            if (decisionMonitor.CurrentDecision != BotQuestingDecision.FollowBoss)
            {
                return updatePreviousState(false);
            }

            setNextAction(BotActionType.FollowBoss, "FollowBoss");
            return updatePreviousState(true);
        }
    }
}
