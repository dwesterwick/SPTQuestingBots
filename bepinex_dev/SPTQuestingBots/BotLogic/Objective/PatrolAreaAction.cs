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

namespace SPTQuestingBots.BotLogic.Objective
{
    internal class RegroupAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private GClass114 baseAction = null;

        public RegroupAction(BotOwner _BotOwner) : base(_BotOwner, 100)
        {
            baseAction = GClass394.CreateNode(BotLogicDecision.simplePatrol, BotOwner);
            baseAction.Awake();
        }

        public override void Update()
        {
            baseAction.Update();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            Vector3 locationOfNearestGroupMember = BotHiveMindMonitor.GetLocationOfNearestGroupMember(BotOwner);
            float targetDistance = (float)ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.TargetRange.Min;
            bool mustRegroup = actionElapsedTime.ElapsedMilliseconds / 1000.0 < ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.MinRegroupTime;

            if (mustRegroup || Vector3.Distance(BotOwner.Position, locationOfNearestGroupMember) > targetDistance + 2)
            {
                RecalculatePath(locationOfNearestGroupMember, targetDistance);
            }
        }
    }
}
