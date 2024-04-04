using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class HoldAtObjectiveAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private float? maxWanderDistance = null;
        private bool wasStuck = false;

        public HoldAtObjectiveAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass460.CreateNode(BotLogicDecision.search, BotOwner));
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();
            BotOwner.Mover.Stop();

            StartActionElapsedTime();

            maxWanderDistance = ObjectiveManager.MaxDistanceForCurrentStep;
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();

            PauseActionElapsedTime();
        }

        public override void Update()
        {
            UpdateBaseAction();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            if (!ObjectiveManager.Position.HasValue || !maxWanderDistance.HasValue)
            {
                throw new InvalidOperationException("Cannot go to a null position");
            }

            ObjectiveManager.StartJobAssigment();

            // This doesn't really need to be updated every frame
            CanSprint = IsAllowedToSprint();

            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    ObjectiveManager.StuckCount++;
                    LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is stuck and will get a new objective.");
                }
                wasStuck = true;

                if (ObjectiveManager.TryChangeObjective())
                {
                    restartStuckTimer();
                }
            }
            else
            {
                wasStuck = false;
            }

            CheckMinElapsedActionTime();

            if (!ObjectiveManager.IsCloseToObjective(maxWanderDistance.Value))
            {
                RecalculatePath(ObjectiveManager.Position.Value);
                return;
            }

            restartStuckTimer();
        }
    }
}
