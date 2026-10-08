using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public abstract class AbstractNavigationPointSelector<T> where T: class, IAICorePointLink
    {
        private const float STALE_THRESHOLD = 0.75f;

        public T? SelectedPoint { get; private set; } = null;

        protected BotOwner Bot;

        private Vector3 _botPositionWhenPointSet = Vector3.negativeInfinity;
        private Vector3 _centerPointPositionWhenPointSet = Vector3.negativeInfinity;
        private Stopwatch timeSincePointCheckedTimer = Stopwatch.StartNew();
        private Stopwatch timeSincePointUpdatedTimer = Stopwatch.StartNew();

        protected virtual float DebounceTimeAfterChecking => 0;
        protected virtual float DebounceTimeAfterUpdating => DebounceTimeAfterChecking;
        protected virtual float MinSearchDistanceBoss => 0;
        protected virtual float MinSearchDistanceFollower => 0;
        protected abstract float MaxSearchDistanceBoss { get; }
        protected abstract float MaxSearchDistanceFollower { get; }

        public bool HasSelectedPoint => SelectedPoint != null;
        public Vector3? SelectedPosition => SelectedPoint?.Position;
        public float DistanceToSelectedPoint => SelectedPoint != null ? Vector3.Distance(Bot.Position, SelectedPoint.Position) : float.NaN;

        protected float MinSearchDistanceBossSqr => MinSearchDistanceBoss * MinSearchDistanceBoss;
        protected float MinSearchDistanceFollowerSqr => MinSearchDistanceFollower * MinSearchDistanceFollower;
        protected float MaxSearchDistanceBossSqr => MaxSearchDistanceBoss * MaxSearchDistanceBoss;
        protected float MaxSearchDistanceFollowerSqr => MaxSearchDistanceFollower * MaxSearchDistanceFollower;
        protected double TimeSincePointChecked => timeSincePointCheckedTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSincePointUpdated => timeSincePointUpdatedTimer.ElapsedMilliseconds / 1000.0;
        
        protected float GetMinSearchDistance() => Bot.HasAQuestingBoss() ? MinSearchDistanceFollower : MinSearchDistanceBoss;
        protected float GetMinSearchDistanceSqr() => Bot.HasAQuestingBoss() ? MinSearchDistanceFollowerSqr : MinSearchDistanceBossSqr;
        protected float GetMaxSearchDistance() => Bot.HasAQuestingBoss() ? MaxSearchDistanceFollower : MaxSearchDistanceBoss;
        protected float GetMaxSearchDistanceSqr() => Bot.HasAQuestingBoss() ? MaxSearchDistanceFollowerSqr : MaxSearchDistanceBossSqr;

        public AbstractNavigationPointSelector(BotOwner botOwner)
        {
            Bot = botOwner;
        }

        protected abstract Vector3 GetCenterPointForSearch();

        protected abstract void Refresh_Internal(Vector3 centerPoint);
        public void Refresh()
        {
            if (TimeSincePointChecked < DebounceTimeAfterChecking)
            {
                return;
            }

            timeSincePointCheckedTimer.Restart();

            if (TimeSincePointUpdated < DebounceTimeAfterUpdating)
            {
                return;
            }

            timeSincePointUpdatedTimer.Restart();

            Vector3 centerPoint = GetCenterPointForSearch();
            Refresh_Internal(centerPoint);
        }

        public void RefreshCoverPointIfStale()
        {
            if (!BotHasMovedBeyondStaleThreshold() && !CenterPointHasMovedBeyondStaleThreshold())
            {
                return;
            }

            Refresh();
        }

        protected bool BotHasMovedBeyondStaleThreshold()
        {
            if (SelectedPoint == null)
            {
                return true;
            }

            float botTravelDistance = Vector3.Distance(Bot.Position, _botPositionWhenPointSet);
            if (botTravelDistance < GetMaxSearchDistance() * STALE_THRESHOLD)
            {
                return false;
            }

            return true;
        }

        protected bool CenterPointHasMovedBeyondStaleThreshold()
        {
            if (SelectedPoint == null)
            {
                return true;
            }

            Vector3 newCenterPoint = GetCenterPointForSearch();
            float distanceToLastCenterPoint = Vector3.Distance(newCenterPoint, _centerPointPositionWhenPointSet);
            if (distanceToLastCenterPoint < GetMaxSearchDistance() * STALE_THRESHOLD)
            {
                return false;
            }

            return true;
        }

        protected void SetSelectedPoint(T? selectedPoint) => SetSelectedPoint(selectedPoint, Vector3.negativeInfinity);

        protected void SetSelectedPoint(T? selectedPoint, Vector3 centerPoint)
        {
            SelectedPoint = selectedPoint;
            _botPositionWhenPointSet = Bot.Position;
            _centerPointPositionWhenPointSet = centerPoint;
        }
    }
}
