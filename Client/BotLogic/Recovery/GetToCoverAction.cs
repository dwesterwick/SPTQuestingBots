using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.BotLogic.Recovery
{
    internal class GetToCoverAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public GetToCoverAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(AIActionsList.CreateNode(BotLogicDecision.simplePatrol, BotOwner));
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();
            UpdateBotMiscActions();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (ObjectiveManager.CoverPointSelector.SelectedPoint == null)
            {
                return;
            }

            CanSprint = !ObjectiveManager.CoverPointSelector.IsNearSelectedPoint && IsAllowedToSprint();

            if (!ObjectiveManager.CoverPointSelector.IsAtSelectedPoint)
            {
                RecalculatePath(ObjectiveManager.CoverPointSelector.SelectedPosition, 0.2f, 0.5f, false, out Models.Pathing.BotPathUpdateNeededReason updateReason);
            }
            else
            {
                restartStuckTimer();
                return;
            }

            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " got stuck while seeking cover");
                }
                wasStuck = true;

                ObjectiveManager.PauseRequest = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.FollowerBreakTime;
                restartStuckTimer();
            }
            else
            {
                wasStuck = false;
            }
        }
    }
}
