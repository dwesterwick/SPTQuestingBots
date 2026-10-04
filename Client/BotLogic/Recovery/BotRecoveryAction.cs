using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace QuestingBots.BotLogic.Recovery
{
    public class BotRecoveryAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        private Stopwatch lookDirectionChangeTimer = Stopwatch.StartNew();
        private float lookDirectionChangeDelay = 0;
        private float MaxHorizontalDegrees = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.HorizontalDeg;
        private float MaxVerticalDegreesDown = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.VerticalDownDeg;
        private float MaxVerticalDegreesUp = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.VerticalUpDeg;
        private float MinLookDirectionChangeDelay = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.DirectionChangeDelay.Min;
        private float MaxLookDirectionChangeDelay = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.DirectionChangeDelay.Max;

        private double ElapsedTimeSinceLastLookDirectionChange => lookDirectionChangeTimer.ElapsedMilliseconds / 1000.0;

        public BotRecoveryAction(BotOwner _BotOwner) : base(_BotOwner, 20)
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
            RefreshLookDirection();
            SetPose();

            BotOwner.Sprint(false);
            BotOwner.StopMove();

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            BotOwner.BotLight.TurnOff(false, true);
            BotOwner.Memory.BotCurrentCoverInfo.TryCheckSafe();
            CheckRemainingAmmo();
        }

        private void RefreshLookDirection()
        {
            if (ElapsedTimeSinceLastLookDirectionChange < lookDirectionChangeDelay)
            {
                return;
            }

            Vector3 toWallVector = ObjectiveManager.CoverPointSelector.SelectedPointToWallVector;
            Vector3 newlookDirection = ChooseRandomLookDirectionAwayFromWall(toWallVector, MaxHorizontalDegrees, MaxVerticalDegreesDown, MaxVerticalDegreesUp);
            BotOwner.Steering.LookToDirection(newlookDirection);

            lookDirectionChangeDelay = UnityEngine.Random.Range(MinLookDirectionChangeDelay, MaxLookDirectionChangeDelay);
            lookDirectionChangeTimer.Restart();
        }

        private Vector3 ChooseRandomLookDirectionAwayFromWall(Vector3 toWallVector, float maxHorizontalDegrees, float maxVerticalDegreesDown, float maxVerticalDegreesUp)
        {
            Vector3 oppositeFromWall = -1 * toWallVector;

            float yawChange = UnityEngine.Random.Range(-maxHorizontalDegrees, maxHorizontalDegrees);
            float pitchChange = UnityEngine.Random.Range(-maxVerticalDegreesDown, maxVerticalDegreesUp);

            Quaternion yawRotation = Quaternion.AngleAxis(yawChange, Vector3.up);

            Vector3 rightAxis = yawRotation * Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(pitchChange, rightAxis);

            Vector3 lookDirection = (yawRotation * pitchRotation * oppositeFromWall).normalized;
            return lookDirection;
        }

        private void SetPose()
        {
            float allowedMinimumPose = IsGroupLeaderQuesting() ? 0.5f : 0.1f;
            float targetPose = ObjectiveManager.CoverPointSelector.GetTargetPoseAtCoverPoint();
            targetPose = Math.Max(allowedMinimumPose, targetPose);

            BotOwner.SetPose(targetPose);
        }

        private bool IsGroupLeaderQuesting()
        {
            BotOwner? groupLeader = BotHiveMindMonitor.GetGroupLeader(BotOwner);
            if (groupLeader == null)
            {
                return false;
            }

            BotObjectiveManager? groupLeaderObjectiveManager = groupLeader.GetObjectiveManager();
            if (groupLeaderObjectiveManager == null)
            {
                return false;
            }

            BotQuestingMonitor groupLeaderQuestingMonitor = groupLeaderObjectiveManager.BotMonitor.GetMonitor<BotQuestingMonitor>();

            return groupLeaderQuestingMonitor.IsQuesting;
        }
    }
}
