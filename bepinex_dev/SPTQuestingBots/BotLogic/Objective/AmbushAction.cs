using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class AmbushAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public AmbushAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass394.CreateNode(BotLogicDecision.holdPosition, BotOwner));
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            RestartActionElapsedTime();
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            UpdateBaseAction();

            if (!ObjectiveManager.IsCloseToObjective())
            {
                UpdateBotSteering();
            }
            else
            {
                if (ObjectiveManager.LookToPosition.HasValue)
                {
                    UpdateBotSteering(ObjectiveManager.LookToPosition.Value);
                }
                else
                {
                    TryLookToLastCorner();
                }
            }

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (!ObjectiveManager.Position.HasValue)
            {
                throw new InvalidOperationException("Cannot go to a null position");
            }

            ObjectiveManager.StartJobAssigment();

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            CheckMinElapsedActionTime();

            if (!ObjectiveManager.IsCloseToObjective())
            {
                RecalculatePath(ObjectiveManager.Position.Value);
                return;
            }

            restartStuckTimer();
        }
    }
}
