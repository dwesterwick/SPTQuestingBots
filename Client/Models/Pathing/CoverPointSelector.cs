using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
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

namespace QuestingBots.Models.Pathing
{
    public class CoverPointSelector : AbstractNavigationPointSelector<CustomNavigationPoint>
    {
        private CoverSearchDefenceData _coverSearchDefenceData;

        protected override float MaxSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MaxSearchDistanceForBosses;
        protected override float MaxSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MaxSearchDistanceForFollowers;
        protected override float MinSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MinSearchDistanceForBosses;
        protected override float MinSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MinSearchDistanceForFollowers;
        protected override float DebounceTimeAfterChecking => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.DebounceTimeAfterChecking;
        protected override float DebounceTimeAfterUpdating => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.DebounceTimeAfterUpdating;

        public CoverLevel SelectedPointCoverLevel => SelectedPoint?.CoverLevel ?? CoverLevel.Stay;
        public Vector3 SelectedPointToWallVector => SelectedPoint?.ToWallVector ?? Vector3.zero;
        public bool IsAtSelectedPoint => DistanceToSelectedPoint <= 1f;
        public bool IsNearSelectedPoint => DistanceToSelectedPoint <= 2f;

        public CoverPointSelector(BotOwner bot) : base(bot)
        {
            _coverSearchDefenceData = new CoverSearchDefenceData(Bot.Settings.FileSettings.Cover.MIN_DEFENCE_LEVEL);
        }

        public void ReserveSelectedCoverPoint()
        {
            //SelectedPoint?.SetOwner(Bot);
            Bot.Memory.SetCoverPoints(SelectedPoint);
        }

        public void ReleaseSelectedCoverPoint()
        {
            SelectedPoint?.SetFree();
            SetSelectedPoint(null);
        }

        public float GetTargetPoseAtCoverPoint()
        {
            switch (SelectedPointCoverLevel)
            {
                case CoverLevel.Stay: return 1f;
                case CoverLevel.Sit: return 0.5f;
                case CoverLevel.Lay: return 0.1f;
            }

            throw new InvalidOperationException("CoverLevel is invalid");
        }

        protected override Vector3 GetCenterPointForSearch()
        {
            if (Bot.HasABoss())
            {
                return Bot.BotFollower.BossToFollow.Position;
            }

            BotObjectiveManager? objectiveManager = Bot.GetObjectiveManager();
            if ((objectiveManager == null) || !objectiveManager.IsQuestingAllowed)
            {
                return Bot.Position;
            }

            if (Bot.GetCurrentQuestingDecision() != EBotQuestingDecision.WaitForAssignment)
            {
                return Bot.Position;
            }

            return objectiveManager.CurrentAssignment?.Position ?? Bot.Position;
        }

        protected override void Refresh_Internal(Vector3 centerPoint)
        {
            float maxSearchDistance = GetMaxSearchDistance();

            if ((SelectedPosition != null) && (Vector3.Distance(centerPoint, SelectedPosition.Value) < maxSearchDistance))
            {
                //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " already has a nearby cover point");
                return;
            }

            ReleaseSelectedCoverPoint();

            CustomNavigationPoint? newCoverPoint = GetNewCoverPoint(centerPoint);
            if (newCoverPoint == null)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Could not find a new cover point for " + _bot.GetText());
                return;
            }

            float distance = Vector3.Distance(centerPoint, newCoverPoint.Position);
            if (distance > maxSearchDistance)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("New cover point for " + _bot.GetText() + " is too far (" + distance + "m)");
                return;
            }

            if (!Bot.Position.HasCompletePathTo(newCoverPoint.Position))
            {
                //Singleton<LoggingUtil>.Instance.LogDebug(Bot.GetText() + " does not have a complete path to new cover point " + distance + "m away");
                return;
            }

            //Singleton<LoggingUtil>.Instance.LogDebug("Found cover point for " + Bot.GetText());
            SetSelectedPoint(newCoverPoint, centerPoint);

            ReserveSelectedCoverPoint();
        }

        private CustomNavigationPoint? GetNewCoverPoint(Vector3 centerPoint)
        {
            int maxIterations = 1000;
            ShootToPoint shootToPoint = Bot.CurrentEnemyTargetPosition(true);
            Vector3? closestFriendCoverPoint = Bot.Covers.ClosestFriendCoverPoint();
            
            CoverSearchData coverSearchData = new CoverSearchData(centerPoint, Bot.CoverSearchInfo, CoverShootType.hide, GetMaxSearchDistanceSqr(),
                GetMinSearchDistanceSqr(), CoverSearchType.distToBotAndToCenter, shootToPoint, closestFriendCoverPoint, null,
                ECheckSHootHide.shootAndHide, _coverSearchDefenceData, PointsArrayType.allWithBush);

            CoverPointEvaluator coverPointEvaluator = new CoverPointEvaluator(coverSearchData);
            return Bot.Covers._аFindByGraph.GetClosestPoint(Bot, centerPoint, false, coverPointEvaluator.IsPointGood, false, maxIterations);
        }

        private class CoverPointEvaluator
        {
            private CoverSearchData _coverSearchData;
            private int _environmentId;

            private Vector3 CenterPosition => _coverSearchData.CenterPos;
            private ICoverSearchBot Bot => _coverSearchData.Bot;

            public CoverPointEvaluator(CoverSearchData coverSearchData)
            {
                _coverSearchData = coverSearchData;
                _environmentId = EnvironmentManagerBase.Instance.TryFindEnvironmentIdByPos(CenterPosition);
            }

            public bool IsPointGood(GroupPoint groupPoint)
            {
                if (!groupPoint.IsFreeById(Bot.Id))
                {
                    return false;
                }

                float distanceSqr = (groupPoint.Position - CenterPosition).sqrMagnitude;
                if (distanceSqr < _coverSearchData.MinDistSqr)
                {
                    return false;
                }
                if (distanceSqr > _coverSearchData.MaxDistSqr)
                {
                    return false;
                }

                if (_environmentId != groupPoint.IdEnvironment)
                {
                    return false;
                }

                return true;
            }
        }
    }
}
