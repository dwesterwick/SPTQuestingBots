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
        protected override float MaxSearchDistance => 25;
        protected override float DebounceTimeForChecking => 0.5f;

        private float MinDistanceFromBoss => 5;
        
        public CoverLevel SelectedPointCoverLevel => SelectedPoint?.CoverLevel ?? CoverLevel.Stay;
        public Vector3 SelectedPointToWallVector => SelectedPoint?.ToWallVector ?? Vector3.zero;
        public bool IsAtSelectedPoint => DistanceToSelectedPoint <= 0.5f;

        public CoverPointSelector(BotOwner bot) : base(bot)
        {

        }

        public void ReserveSelectedCoverPoint()
        {
            _bot.Memory.SetCoverPoints(SelectedPoint);
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

        private bool HasAQuestingBoss()
        {
            BotObjectiveManager? objectiveManager = _bot.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not get BotObjectiveManager for " + _bot.GetText());
                return false;
            }

            BotQuestingDecisionMonitor decisionMonitor = objectiveManager.BotMonitor.GetMonitor<BotQuestingDecisionMonitor>();
            return decisionMonitor.HasAQuestingBoss;
        }

        protected override Vector3 GetCenterPointForSearch()
        {
            return HasAQuestingBoss() ? _bot.BotFollower.BossToFollow.Position : _bot.Position;
        }

        protected override void Refresh_Internal(Vector3 centerPoint)
        {
            if ((SelectedPosition != null) && (Vector3.Distance(centerPoint, SelectedPosition.Value) < MaxSearchDistance))
            {
                //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " already has a nearby cover point");
                return;
            }

            SetSelectedPoint(null);

            CustomNavigationPoint? newCoverPoint = GetNewCoverPoint(centerPoint);
            if (newCoverPoint == null)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Could not find a new cover point for " + _bot.GetText());
                return;
            }

            float distance = Vector3.Distance(centerPoint, newCoverPoint.Position);
            if (distance > MaxSearchDistance)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("New cover point for " + _bot.GetText() + " is too far (" + distance + "m)");
                return;
            }

            if (!_bot.Position.HasCompletePathTo(newCoverPoint.Position))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " does not have a complete path to new cover point " + distance + "m away");
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Found cover point for " + _bot.GetText());
            SetSelectedPoint(newCoverPoint);

            ReserveSelectedCoverPoint();
        }

        private CustomNavigationPoint? GetNewCoverPoint(Vector3 centerPoint)
        {
            CoverSearchDefenceData coverSearchDefenceData = new CoverSearchDefenceData(_bot.Settings.FileSettings.Cover.MIN_DEFENCE_LEVEL);
            Vector3? closestFriendCoverPoint = _bot.Covers.ClosestFriendCoverPoint();
            float minDistanceSqr = HasAQuestingBoss() ? MinDistanceFromBoss * MinDistanceFromBoss : MinSearchDistanceSqr;

            CoverSearchData coverSearchData = new CoverSearchData(centerPoint, _bot.CoverSearchInfo, CoverShootType.hide, MaxSearchDistanceSqr,
                minDistanceSqr, CoverSearchType.distToBotAndToCenter, _bot.CurrentEnemyTargetPosition(true), closestFriendCoverPoint, null,
                ECheckSHootHide.shootAndHide, coverSearchDefenceData, PointsArrayType.allWithBush);

            CustomNavigationPoint newCoverPoint = _bot.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);

            return newCoverPoint;
        }
    }
}
