using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.BotLogic.Investigate
{
    internal class LookAtSoundAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public LookAtSoundAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(AIActionsList.CreateNode(BotLogicDecision.holdPosition, BotOwner));
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
            BotOwner.Sprint(false);
            BotOwner.StopMove();

            TryLookAtSoundPosition();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            CheckRemainingAmmo();
        }

        private bool TryLookAtSoundPosition()
        {
            BotHearingMonitor hearingMonitor = ObjectiveManager.BotMonitor.GetMonitor<BotHearingMonitor>();
            Vector3? positionToInvestigate = hearingMonitor.LastEstimatedSoundPosition;
            if (positionToInvestigate == null)
            {
                return false;
            }

            UpdateBotSteering(positionToInvestigate.Value);
            return true;
        }
    }
}
