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
        public T? SelectedPoint { get; private set; } = null;

        protected BotOwner _bot;

        private Vector3 _botPositionWhenPointSet = Vector3.negativeInfinity;
        private Stopwatch timeSincePointCheckedTimer = Stopwatch.StartNew();
        private Stopwatch timeSincePointSetTimer = Stopwatch.StartNew();

        protected virtual float DebounceTimeForChecking => 0;
        protected virtual float DebounceTimeForSetting => DebounceTimeForChecking;
        protected virtual float MinSearchDistance => 0;

        protected abstract float MaxSearchDistance { get; }

        public bool HasSelectedPoint => SelectedPoint != null;
        public Vector3? SelectedPosition => SelectedPoint?.Position;
        public float DistanceToSelectedPoint => SelectedPoint != null ? Vector3.Distance(_bot.Position, SelectedPoint.Position) : float.NaN;

        protected float MinSearchDistanceSqr => MinSearchDistance * MinSearchDistance;
        protected float MaxSearchDistanceSqr => MaxSearchDistance * MaxSearchDistance;
        protected double TimeSincePointChecked => timeSincePointCheckedTimer.ElapsedMilliseconds / 1000.0;
        protected double TimeSincePointSet => timeSincePointSetTimer.ElapsedMilliseconds / 1000.0;

        public AbstractNavigationPointSelector(BotOwner botOwner)
        {
            _bot = botOwner;
        }

        protected abstract Vector3 GetCenterPointForSearch();

        protected abstract void Refresh_Internal(Vector3 centerPoint);
        public void Refresh()
        {
            if (TimeSincePointChecked < DebounceTimeForChecking)
            {
                return;
            }

            timeSincePointCheckedTimer.Restart();

            if (TimeSincePointSet < DebounceTimeForSetting)
            {
                return;
            }

            timeSincePointSetTimer.Restart();

            Vector3 centerPoint = GetCenterPointForSearch();
            Refresh_Internal(centerPoint);
        }

        public void RefreshCoverPointIfStale()
        {
            float distanceToLastSelectedPoint = Vector3.Distance(_bot.Position, _botPositionWhenPointSet);
            if ((SelectedPoint != null) && (distanceToLastSelectedPoint < MaxSearchDistance / 2))
            {
                return;
            }

            Refresh();
        }

        protected void SetSelectedPoint(T? selectedPoint)
        {
            SelectedPoint = selectedPoint;
            _botPositionWhenPointSet = _bot.Position;
        }
    }
}
