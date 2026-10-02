using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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

            CanSprint = IsAllowedToSprint();

            if (!ObjectiveManager.CoverPointSelector.IsAtSelectedPoint)
            {
                RecalculatePath(ObjectiveManager.CoverPointSelector.SelectedPosition);
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

                restartStuckTimer();
            }
            else
            {
                wasStuck = false;
            }
        }
    }
}
