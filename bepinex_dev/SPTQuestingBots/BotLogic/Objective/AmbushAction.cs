using EFT;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class AmbushAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool allowedToIgnoreHearing = true;
        private bool isIgnoringHearing = false;

        public AmbushAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass522.CreateNode(BotLogicDecision.holdPosition, BotOwner));
        }

        public AmbushAction(BotOwner _BotOwner, bool _allowedToIgnoreHearing) : this(_BotOwner)
        {
            allowedToIgnoreHearing = _allowedToIgnoreHearing;
        }

        public override void Start()
        {
            base.Start();

            BotOwner.PatrollingData.Pause();

            StartActionElapsedTime();
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.PatrollingData.Unpause();

            PauseActionElapsedTime();

            // If the bot was instructed to ignore its hearing, reverse the instruction so it can be effective in combat again
            if (allowedToIgnoreHearing && isIgnoringHearing)
            {
                ObjectiveManager.BotMonitor.GetMonitor<BotHearingMonitor>().TrySetIgnoreHearing((float)ActionElapsedTimeRemaining, false);
                isIgnoringHearing = false;
            }
        }

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            UpdateBaseAction(data);

            // While the bot is moving to the ambush position, have it look where it's going. Once at the ambush position, have it look to the
            // a specific location if defined by the quest. Otherwise, have it look where it just came from. 
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
                isIgnoringHearing = false;

                return;
            }

            // Needed in case somebody drops the layer priorities of this mod. Without doing this, SAIN will prevent bots from staying in their ambush spots.
            if (allowedToIgnoreHearing && !isIgnoringHearing)
            {
                ObjectiveManager.BotMonitor.GetMonitor<BotHearingMonitor>().TrySetIgnoreHearing((float)ActionElapsedTimeRemaining, true);
                isIgnoringHearing = true;
            }

            restartStuckTimer();
        }
    }
}
