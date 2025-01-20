using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class FollowerRegroupAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public FollowerRegroupAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass507.CreateNode(BotLogicDecision.simplePatrol, BotOwner));
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update()
        {
            UpdateBotMovement(CanSprint);
            UpdateBotSteering();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            CanSprint = IsAllowedToSprint();

            // Force the bot to regroup for a certain amount of time after starting this action
            bool mustRegroup = ActionElpasedTime < ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.MinRegroupTime;

            // Determine where the bot should go
            Vector3? targetLocation = getTargetPosition();
            if (!targetLocation.HasValue)
            {
                return;
            }

            // Check if the bot should continue regrouping
            float targetDistance = (float)ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat.Min;
            if (mustRegroup || Vector3.Distance(BotOwner.Position, targetLocation.Value) > targetDistance)
            {
                RecalculatePath(targetLocation.Value, targetDistance);
            }
            else
            {
                ObjectiveManager.PauseRequest = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.RegroupPauseTime;
            }

            // Check if the bot is unable to reach its boss. If so, fall back to the default EFT layer for a bit. 
            if (checkIfBotIsStuck())
            {
                if (!wasStuck)
                {
                    LoggingController.LogWarning("Follower " + BotOwner.GetText() + " got stuck while trying to group with its boss");
                }
                wasStuck = true;

                restartStuckTimer();
            }
            else
            {
                wasStuck = false;
            }
        }

        private Vector3? getTargetPosition()
        {
            BotOwner boss = BotHiveMindMonitor.GetBoss(BotOwner);
            if (boss == null)
            {
                return null;
            }

            return boss.Position;
        }
    }
}
