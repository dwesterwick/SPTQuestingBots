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
    internal class BossRegroupAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private bool wasStuck = false;

        public BossRegroupAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass522.CreateNode(BotLogicDecision.simplePatrol, BotOwner));
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

            CanSprint = IsAllowedToSprint();

            // Force the bot to regroup for a certain amount of time after starting this action
            bool mustRegroup = ActionElpasedTime < ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.MinRegroupTime;

            // determine the location of the nearest follower and the target distance to it
            Vector3 locationOfNearestGroupMember = BotHiveMindMonitor.GetLocationOfNearestGroupMember(BotOwner);
            float targetDistance = (float)ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeQuesting.Min;
            
            // Check if the bot should find its nearest follower
            if (mustRegroup || Vector3.Distance(BotOwner.Position, locationOfNearestGroupMember) > targetDistance + 2)
            {
                float allowedVariation = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetPositionVariationAllowed;
                RecalculatePath(locationOfNearestGroupMember, allowedVariation, targetDistance);
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
                    IReadOnlyCollection<BotOwner> followers = HiveMind.BotHiveMindMonitor.GetFollowers(BotOwner);
                    string followersText = string.Join(", ", followers.Select(f => f.GetText()));

                    LoggingController.LogWarning("Boss " + BotOwner.GetText() + " has been waiting for his followers (" + followersText + ") for a long time...");
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
