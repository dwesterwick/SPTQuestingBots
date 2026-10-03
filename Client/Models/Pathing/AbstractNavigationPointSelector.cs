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
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public abstract class AbstractNavigationPointSelector<T> where T: class, IAICorePointLink
    {
        public T? SelectedPoint { get; private set; } = null;

        protected BotOwner Bot;

        private Vector3 _botPositionWhenPointSet = Vector3.negativeInfinity;
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
        protected bool HasBoss => Bot.BotFollower.HaveBoss && Bot.BotFollower.BossToFollow.IsAlive;

        protected float GetMinSearchDistance() => HasAQuestingBoss() ? MinSearchDistanceFollower : MinSearchDistanceBoss;
        protected float GetMinSearchDistanceSqr() => HasAQuestingBoss() ? MinSearchDistanceFollowerSqr : MinSearchDistanceBossSqr;
        protected float GetMaxSearchDistance() => HasAQuestingBoss() ? MaxSearchDistanceFollower : MaxSearchDistanceBoss;
        protected float GetMaxSearchDistanceSqr() => HasAQuestingBoss() ? MaxSearchDistanceFollowerSqr : MaxSearchDistanceBossSqr;

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
            float distanceToLastSelectedPoint = Vector3.Distance(Bot.Position, _botPositionWhenPointSet);
            if ((SelectedPoint != null) && (distanceToLastSelectedPoint < GetMaxSearchDistance() / 2))
            {
                return;
            }

            Refresh();
        }

        protected void SetSelectedPoint(T? selectedPoint)
        {
            SelectedPoint = selectedPoint;
            _botPositionWhenPointSet = Bot.Position;
        }

        protected bool HasAQuestingBoss()
        {
            if (!HasBoss)
            {
                return false;
            }

            BotObjectiveManager? objectiveManager = Bot.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not get BotObjectiveManager for " + Bot.GetText());
                return false;
            }

            BotQuestingDecisionMonitor decisionMonitor = objectiveManager.BotMonitor.GetMonitor<BotQuestingDecisionMonitor>();
            return decisionMonitor.HasAQuestingBoss;
        }
    }
}
