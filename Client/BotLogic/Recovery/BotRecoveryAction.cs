using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private float LookDirectionRandomness = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.DirectionRandomness;
        private float MaxLookRotationSpeed = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.LookAroundLimits.MaxRotationSpeed;

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
            //BotOwner.Memory.BotCurrentCoverInfo.TryCheckSafe();
            CheckRemainingAmmo();
        }

        private void RefreshLookDirection()
        {
            if (ElapsedTimeSinceLastLookDirectionChange < lookDirectionChangeDelay)
            {
                return;
            }

            Vector3 newlookDirection = GetRandomLookDirectionTowardAPlayer() ?? GetRandomLookDirection();
            BotOwner.Steering.LookToDirection(newlookDirection, MaxLookRotationSpeed);

            lookDirectionChangeDelay = UnityEngine.Random.Range(MinLookDirectionChangeDelay, MaxLookDirectionChangeDelay);
            lookDirectionChangeTimer.Restart();
        }

        private Vector3? GetRandomLookDirectionTowardAPlayer()
        {
            Vector3 oppositeFromWallVector = -1 * ObjectiveManager.CoverPointSelector.SelectedPointToWallVector;
            Vector3[] eligibleLookDirections = GetDirectionsToAllPlayers()
                .ApplyRandomOffsets(LookDirectionRandomness, LookDirectionRandomness, LookDirectionRandomness)
                .ClampToBeWithin(oppositeFromWallVector, MaxHorizontalDegrees, MaxVerticalDegreesDown, MaxVerticalDegreesUp)
                .ToArray();

            if (eligibleLookDirections.Length == 0)
            {
                return null;
            }

            Vector3 randomDirection = eligibleLookDirections.RandomElement();
            return randomDirection;
        }

        private Vector3 GetRandomLookDirection()
        {
            Vector3 oppositeFromWallVector = -1 * ObjectiveManager.CoverPointSelector.SelectedPointToWallVector;
            return oppositeFromWallVector.ChooseRandomDirectionAround(MaxHorizontalDegrees, MaxVerticalDegreesDown, MaxVerticalDegreesUp);
        }

        private int GetNumberOfPlayersWithinLimits()
        {
            int playersWithinLimits = GetDirectionsToAllPlayersWithinLimits().CountNonAlloc();
            return playersWithinLimits;
        }

        private IEnumerable<Vector3> GetDirectionsToAllPlayersWithinLimits()
        {
            Vector3 oppositeFromWallVector = -1 * ObjectiveManager.CoverPointSelector.SelectedPointToWallVector;
            return GetDirectionsToAllPlayers().AreWithinHorizonalLimitsOf(oppositeFromWallVector, MaxHorizontalDegrees);
        }

        private IEnumerable<Vector3> GetDirectionsToAllPlayers()
        {
            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                Vector3 vectorToBot = (player.Position - BotOwner.Position).normalized;
                yield return vectorToBot;
            }
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
