using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public class PatrolPointSelector : AbstractNavigationPointSelector<PatrolPoint>
    {
        protected override float MaxSearchDistance => (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointRadiusAroundBoss.Max;
        protected override float MinSearchDistance => HasBoss ? (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointRadiusAroundBoss.Min : 0;
        protected override float DebounceTimeForSetting => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.DebounceTimeAfterChangingPatrolPoint;

        private PatrolWay? _selectedWay = null;

        protected bool HasBoss => _bot.BotFollower.HaveBoss && _bot.BotFollower.BossToFollow.IsAlive;

        public bool HasWay => _selectedWay != null;
        public bool IsWayReserved => _selectedWay?.PatrolType == PatrolType.reserved;

        public PatrolPointSelector(BotOwner bot) : base(bot)
        {
            
        }

        public void ReleasePatrolPointReservation()
        {
            _bot.PatrollingData.PointControl._lastSetOwner.SetOwner(null);
        }

        protected override Vector3 GetCenterPointForSearch()
        {
            if (HasBoss)
            {
                return _bot.BotFollower.BossToFollow.Position;
            }

            return _bot.Position;
        }

        protected override void Refresh_Internal(Vector3 centerPoint)
        {
            SetSelectedPoint(null);

            _selectedWay = GetClosestPatrolWay(centerPoint, out PatrolPoint? closestPatrolPoint);

            if (closestPatrolPoint != null)
            {
                float distance = Vector3.Distance(centerPoint, closestPatrolPoint.Position);
                if (!_bot.Position.HasCompletePathTo(closestPatrolPoint.Position))
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " does not have a complete path to selected patrol point " + distance + "m away");
                }

                _selectedWay = null;
                closestPatrolPoint = null;
            }

            SetSelectedPoint(closestPatrolPoint);

            if ((_selectedWay != null) && (_bot.PatrollingData.PointControl.Way != _selectedWay))
            {
                _bot.PatrollingData.PointControl.SetWay(_selectedWay, new FindNextPointDelegate(CreatePointContainer));
                //Singleton<LoggingUtil>.Instance.LogInfo("Patrol way changed for " + _bot.GetText());
            }
            else
            {
                PatrolPointContainer? container = CreatePointContainer();
                _bot.PatrollingData.PointControl.SetTarget(container, -1);
            }

            _bot.PatrollingData.PointControl.SetPatrolPointOwner(_bot.PatrollingData.PointControl.PatrolPoint.TargetPoint);
        }

        // for FindNextPointDelegate
        private PatrolPointContainer CreatePointContainer(bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, PatrolPointFilterDelegate? pointFilter = null)
        {
            return CreatePointContainer() ?? _bot.PatrollingData.PointChooser.FindNextPoint(withSetting, withoutNext, minSubTargets, canCut, pointFilter);
        }

        private PatrolPointContainer? CreatePointContainer() => SelectedPoint == null ? null : new PatrolPointContainer(SelectedPoint);

        private PatrolWay? GetClosestPatrolWay(Vector3 centerPoint, out PatrolPoint? closestPatrolPoint)
        {
            closestPatrolPoint = null;
            PatrolWay? closestWay = null;
            float closestPointDistance = float.MaxValue;

            foreach (PatrolWay way in _bot.BotsGroup.BotZone.PatrolWays)
            {
                if (!_bot.CanUsePatrolWay(way))
                {
                    continue;
                }

                PatrolPoint? closestPoint = GetClosestPatrolPoint(centerPoint);
                if (closestPoint == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(closestPoint.Position, _bot.Position);
                if (distance > MaxSearchDistance)
                {
                    continue;
                }

                if (distance < closestPointDistance)
                {
                    closestPointDistance = distance;
                    closestWay = way;
                    closestPatrolPoint = closestPoint;
                }
            }

            return closestWay;
        }

        private PatrolPoint? GetClosestPatrolPoint(Vector3 centerPoint)
        {
            float closestPointDistance = float.MaxValue;
            PatrolPoint? closestPoint = null;
            foreach (PatrolPoint patrolPoint in _bot.PatrollingData.PointControl.Way.Points)
            {
                if (!patrolPoint.IsFreeFor(_bot))
                {
                    //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " cannot use patrol point at " + patrolPoint.Position + "; reserved for " + patrolPoint.Owner.GetText());
                    continue;
                }

                float distance = Vector3.Distance(patrolPoint.Position, centerPoint);
                if (distance > MaxSearchDistance)
                {
                    continue;
                }

                if (distance <= MinSearchDistance)
                {
                    continue;
                }

                if (distance < closestPointDistance)
                {
                    closestPointDistance = distance;
                    closestPoint = patrolPoint;
                }
            }

            return closestPoint;
        }
    }
}
