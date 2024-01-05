using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class RegroupAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public RegroupAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            SetBaseAction(GClass394.CreateNode(BotLogicDecision.simplePatrol, BotOwner));
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

            // determine the location of the nearest follower and the target distance to it
            Vector3 locationOfNearestGroupMember = BotHiveMindMonitor.GetLocationOfNearestGroupMember(BotOwner);
            float targetDistance = (float)ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRange.Min;
            
            // Check if the bot should find its nearest follower
            if (mustRegroup || Vector3.Distance(BotOwner.Position, locationOfNearestGroupMember) > targetDistance + 2)
            {
                RecalculatePath(locationOfNearestGroupMember, targetDistance);
            }
        }
    }
}
