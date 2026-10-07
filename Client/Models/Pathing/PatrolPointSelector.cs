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
            Bot.PatrollingData.PointControl._lastSetOwner?.SetOwner(null);
        }

        public bool CanUseSelectedPoint()
        {
            if (SelectedPosition == null)
            {
                return false;
            }

            Vector3 centerPoint = GetCenterPointForSearch();
            float distanceToNewPoint = Vector3.Distance(centerPoint, SelectedPosition.Value);

            return distanceToNewPoint < GetMaxSearchDistance();
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
            SetSelectedPoint(null);

            _selectedWay = GetClosestPatrolWay(centerPoint, out PatrolPoint? closestPatrolPoint);

            if ((_selectedWay == null) || (closestPatrolPoint == null))
            {
                return;
            }

            float distance = Vector3.Distance(centerPoint, closestPatrolPoint.Position);
            if (!Bot.Position.HasCompletePathTo(closestPatrolPoint.Position))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(Bot.GetText() + " does not have a complete path to selected patrol point " + distance + "m away");
                return;
            }

            SetSelectedPoint(closestPatrolPoint);
            SetPatrolPointTarget();
        }

        // for FindNextPointDelegate
        private PatrolPointContainer CreatePointContainer(bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, PatrolPointFilterDelegate? pointFilter = null)
        {
            return CreatePointContainer() ?? Bot.PatrollingData.PointChooser.FindNextPoint(withSetting, withoutNext, minSubTargets, canCut, pointFilter);
        }

        private PatrolPointContainer? CreatePointContainer() => SelectedPoint == null ? null : new PatrolPointContainer(SelectedPoint);

        private void SetPatrolPointTarget()
        {
            if (Bot.PatrollingData.PointControl.Way != _selectedWay)
            {
                Bot.PatrollingData.PointControl.SetWay(_selectedWay, new FindNextPointDelegate(CreatePointContainer));
                //Singleton<LoggingUtil>.Instance.LogInfo("Patrol way changed for " + _bot.GetText());
            }

            PatrolPointContainer? container = CreatePointContainer();

            int index = -1;
            Bot.PatrollingData.PointControl.SetTarget(container, index);

            //float distanceToNewPoint = Vector3.Distance(centerPoint, Bot.PatrollingData.PointControl.PatrolPoint.Position);
            //Singleton<LoggingUtil>.Instance.LogInfo("Set patrol point " + Bot.GetText() + " " + distanceToNewPoint + "m away");
        }

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

                PatrolPoint? closestPoint = GetClosestPatrolPoint(centerPoint, way);
                if (closestPoint == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(closestPoint.Position, centerPoint);
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

        private PatrolPoint? GetClosestPatrolPoint(Vector3 centerPoint, PatrolWay way)
        {
            float closestPointDistance = float.MaxValue;
            PatrolPoint? closestPoint = null;

            float maxSearchDistance = GetMaxSearchDistance();
            float minSearchDistance = GetMinSearchDistance();

            foreach (PatrolPoint patrolPoint in way.Points)
            {
                if (!IsPatrolPointEligible(patrolPoint, centerPoint))
                {
                    continue;
                }

                float distance = Vector3.Distance(patrolPoint.Position, centerPoint);
                if (distance < closestPointDistance)
                {
                    closestPointDistance = distance;
                    closestPoint = patrolPoint;
                }
            }

            return closestPoint;
        }

        private bool IsPatrolPointEligible(PatrolPoint patrolPoint, Vector3 centerPoint)
        {
            float maxSearchDistance = GetMaxSearchDistance();
            float minSearchDistance = GetMinSearchDistance();

            if (!patrolPoint.IsFreeFor(Bot))
            {
                //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " cannot use patrol point at " + subPoint.Position + "; reserved for " + subPoint.Owner.GetText());
                return false;
            }

            float distance = Vector3.Distance(patrolPoint.Position, centerPoint);
            if (distance > maxSearchDistance)
            {
                return false;
            }

            if (distance < minSearchDistance)
            {
                return false;
            }

            return true;
        }
    }
}
