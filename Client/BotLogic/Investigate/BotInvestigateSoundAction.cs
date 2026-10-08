using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.BotLogic.Investigate
{
    public class BotInvestigateSoundAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public BotInvestigateSoundAction(BotOwner _BotOwner) : base(_BotOwner, 100)
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
            UpdateBotMovement(false);
            UpdateBotSteering();
            UpdateBotMiscActions();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            BotHearingMonitor hearingMonitor = ObjectiveManager.BotMonitor.GetMonitor<BotHearingMonitor>();
            Vector3? positionToInvestigate = hearingMonitor.LastEstimatedSoundPosition;
            if (positionToInvestigate == null)
            {
                return;
            }

            if (!hearingMonitor.IsAtLastEstimatedSoundPosition)
            {
                RecalculatePath(positionToInvestigate);
            }
            else
            {
                hearingMonitor.IgnoreMostRecentSound();

                restartStuckTimer();
                return;
            }

            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    Singleton<LoggingUtil>.Instance.LogWarning(BotOwner.GetText() + " got stuck while investigating a nearby sound");
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
