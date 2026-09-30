using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.BotMonitor;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public class CoverPointSelector
    {
        private BotOwner _bot;

        private CustomNavigationPoint? _coverPoint = null;

        private float MinDistanceFromBoss => 5;
        private float MaxDistance => 25;
        private float MaxDistanceSqr => MaxDistance * MaxDistance;

        public bool HasCoverPoint => _coverPoint != null;
        public Vector3? CoverPoint => _coverPoint?.Position;
        public CoverLevel CoverLevel => _coverPoint?.CoverLevel ?? CoverLevel.Stay;
        public Vector3 ToWallVector => _coverPoint?.ToWallVector ?? Vector3.zero;
        public float DistanceToCoverPoint => _coverPoint != null ? Vector3.Distance(_bot.Position, _coverPoint.Position) : float.NaN;
        public bool IsAtCoverPoint => DistanceToCoverPoint <= 0.5f;

        public CoverPointSelector(BotOwner bot)
        {
            _bot = bot;
        }

        public void ReserveSelectedCoverPoint()
        {
            _bot.Memory.SetCoverPoints(_coverPoint);
        }

        public float GetTargetPoseAtCoverPoint()
        {
            switch (CoverLevel)
            {
                case CoverLevel.Stay: return 1f;
                case CoverLevel.Sit: return 0.5f;
                case CoverLevel.Lay: return 0.1f;
            }

            throw new InvalidOperationException("CoverLevel is invalid");
        }

        public void RefreshCoverPoint()
        {
            Vector3 centerPoint = GetCenterPoint();
            RefreshCoverPoint(centerPoint);
        }

        private Vector3 GetCenterPoint()
        {
            return HasAQuestingBoss() ? _bot.BotFollower.BossToFollow.Position : _bot.Position;
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

        public void RefreshCoverPoint(Vector3 centerPoint)
        {
            if ((CoverPoint != null) && (Vector3.Distance(centerPoint, CoverPoint.Value) < MaxDistance))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " already has a nearby cover point");
                return;
            }

            _coverPoint = null;

            CustomNavigationPoint? newCoverPoint = GetNewCoverPoint(centerPoint);
            if (newCoverPoint == null)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Could not find a new cover point for " + _bot.GetText());
                return;
            }

            float distance = Vector3.Distance(centerPoint, newCoverPoint.Position);
            if (distance > MaxDistance)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("New cover point for " + _bot.GetText() + " is too far (" + distance + "m)");
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Found cover point for " + _bot.GetText());
            _coverPoint = newCoverPoint;

            ReserveSelectedCoverPoint();
        }

        private CustomNavigationPoint? GetNewCoverPoint(Vector3 centerPoint)
        {
            CoverSearchDefenceData coverSearchDefenceData = new CoverSearchDefenceData(_bot.Settings.FileSettings.Cover.MIN_DEFENCE_LEVEL);
            Vector3? closestFriendCoverPoint = _bot.Covers.ClosestFriendCoverPoint();
            float minDistanceSqr = HasAQuestingBoss() ? MinDistanceFromBoss * MinDistanceFromBoss : 0;

            CoverSearchData coverSearchData = new CoverSearchData(centerPoint, _bot.CoverSearchInfo, CoverShootType.hide, MaxDistanceSqr, minDistanceSqr, CoverSearchType.distToBotAndToCenter, _bot.CurrentEnemyTargetPosition(true), closestFriendCoverPoint, null, ECheckSHootHide.shootAndHide, coverSearchDefenceData, PointsArrayType.allWithBush);

            CustomNavigationPoint newCoverPoint = _bot.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);

            return newCoverPoint;
        }
    }
}
