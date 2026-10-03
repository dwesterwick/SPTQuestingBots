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
        protected override float MaxSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.MaxSearchDistanceForBosses;
        protected override float MaxSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.MaxSearchDistanceForFollowers;
        protected override float MinSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.MinSearchDistanceForBosses;
        protected override float MinSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.MinSearchDistanceForFollowers;
        protected override float DebounceTimeAfterChecking => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.DebounceTimeAfterChecking;
        protected override float DebounceTimeAfterUpdating => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointUpdates.DebounceTimeAfterUpdating;

        private PatrolWay? _selectedWay = null;

        public bool HasWay => _selectedWay != null;
        public bool IsWayReserved => _selectedWay?.PatrolType == PatrolType.reserved;

        public PatrolPointSelector(BotOwner bot) : base(bot)
        {
            
        }

        public void ReleasePatrolPointReservation()
        {
            Bot.PatrollingData.PointControl._lastSetOwner.SetOwner(null);
        }

        protected override Vector3 GetCenterPointForSearch() => HasBoss ? Bot.BotFollower.BossToFollow.Position : Bot.Position;

        protected override void Refresh_Internal(Vector3 centerPoint)
        {
            SetSelectedPoint(null);

            _selectedWay = GetClosestPatrolWay(centerPoint, out PatrolPoint? closestPatrolPoint);

            if (closestPatrolPoint != null)
            {
                float distance = Vector3.Distance(centerPoint, closestPatrolPoint.Position);
                if (!Bot.Position.HasCompletePathTo(closestPatrolPoint.Position))
                {
                    Singleton<LoggingUtil>.Instance.LogDebug(Bot.GetText() + " does not have a complete path to selected patrol point " + distance + "m away");
                }

                _selectedWay = null;
                closestPatrolPoint = null;
            }

            SetSelectedPoint(closestPatrolPoint);

            if ((_selectedWay != null) && (Bot.PatrollingData.PointControl.Way != _selectedWay))
            {
                Bot.PatrollingData.PointControl.SetWay(_selectedWay, new FindNextPointDelegate(CreatePointContainer));
                //Singleton<LoggingUtil>.Instance.LogInfo("Patrol way changed for " + _bot.GetText());
            }
            else
            {
                PatrolPointContainer? container = CreatePointContainer();
                Bot.PatrollingData.PointControl.SetTarget(container, -1);
            }

            Bot.PatrollingData.PointControl.SetPatrolPointOwner(Bot.PatrollingData.PointControl.PatrolPoint.TargetPoint);
        }

        // for FindNextPointDelegate
        private PatrolPointContainer CreatePointContainer(bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, PatrolPointFilterDelegate? pointFilter = null)
        {
            return CreatePointContainer() ?? Bot.PatrollingData.PointChooser.FindNextPoint(withSetting, withoutNext, minSubTargets, canCut, pointFilter);
        }

        private PatrolPointContainer? CreatePointContainer() => SelectedPoint == null ? null : new PatrolPointContainer(SelectedPoint);

        private PatrolWay? GetClosestPatrolWay(Vector3 centerPoint, out PatrolPoint? closestPatrolPoint)
        {
            closestPatrolPoint = null;
            PatrolWay? closestWay = null;
            float closestPointDistance = float.MaxValue;

            float maxSearchDistance = GetMaxSearchDistance();

            foreach (PatrolWay way in Bot.BotsGroup.BotZone.PatrolWays)
            {
                if (!Bot.CanUsePatrolWay(way))
                {
                    continue;
                }

                PatrolPoint? closestPoint = GetClosestPatrolPoint(centerPoint);
                if (closestPoint == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(closestPoint.Position, Bot.Position);
                if (distance > maxSearchDistance)
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

            float maxSearchDistance = GetMaxSearchDistance();
            float minSearchDistance = GetMinSearchDistance();

            foreach (PatrolPoint patrolPoint in Bot.PatrollingData.PointControl.Way.Points)
            {
                if (!patrolPoint.IsFreeFor(Bot))
                {
                    //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " cannot use patrol point at " + patrolPoint.Position + "; reserved for " + patrolPoint.Owner.GetText());
                    continue;
                }

                float distance = Vector3.Distance(patrolPoint.Position, centerPoint);
                if (distance > maxSearchDistance)
                {
                    continue;
                }

                if (distance <= minSearchDistance)
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
