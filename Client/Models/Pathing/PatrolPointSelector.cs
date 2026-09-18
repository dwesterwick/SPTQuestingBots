using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public class PatrolPointSelector
    {
        private BotOwner _bot;
        private PatrolPoint? _selectedPoint = null;

        public PatrolPointSelector(BotOwner bot)
        {
            _bot = bot;
        }

        public void RefreshPatrolPoint()
        {
            _selectedPoint = null;

            PatrolWay? closestWay = GetClosestPatrolWay();
            if (closestWay == null)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("Could not find patrol point for " + _bot.GetText());
            }

            if ((closestWay != null) && (_bot.PatrollingData.PointControl.Way != closestWay))
            {
                _bot.PatrollingData.PointControl.SetWay(closestWay, new FindNextPointDelegate(CreatePointContainer));
                //Singleton<LoggingUtil>.Instance.LogInfo("Patrol way changed for " + _bot.GetText());
            }
            else
            {
                PatrolPointContainer? container = CreatePointContainer();
                _bot.PatrollingData.PointControl.SetTarget(container, -1);
            }
            
            _bot.PatrollingData.PointControl.SetPatrolPointOwner(_bot.PatrollingData.PointControl.PatrolPoint.TargetPoint);

            if (closestWay == null)
            {
                return;
            }

            //float distance = Vector3.Distance(_bot.PatrollingData.PointControl.PatrolPoint.TargetPoint.Position, _bot.Position);
            //Singleton<LoggingUtil>.Instance.LogDebug("Setting new patrol point " + distance + "m away for " + _bot.GetText() + " (" + _bot.PatrollingData.PointControl.PatrolPoint.TargetPoint.Position + ")");
        }

        private PatrolPointContainer CreatePointContainer(bool withSetting, bool withoutNext, int minSubTargets = -1, bool canCut = true, PatrolPointFilterDelegate? pointFilter = null)
        {
            return CreatePointContainer() ?? _bot.PatrollingData.PointChooser.FindNextPoint(withSetting, withoutNext, minSubTargets, canCut, pointFilter);
        }

        private PatrolPointContainer? CreatePointContainer()
        {
            if (_selectedPoint != null)
            {
                return new PatrolPointContainer(_selectedPoint);
            }

            return null;
        }

        private PatrolWay? GetClosestPatrolWay()
        {
            PatrolWay? closestWay = null;
            float closestPointDistance = float.MaxValue;

            float maxPatrolPointDistance = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointRadiusAroundBoss.Max;
            float bossExclusionRadius = (float)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.PatrolPointRadiusAroundBoss.Min;

            foreach (PatrolWay way in _bot.BotsGroup.BotZone.PatrolWays)
            {
                if (!_bot.CanUsePatrolWay(way))
                {
                    continue;
                }

                PatrolPoint? closestPoint = GetClosestPatrolPointNearBoss(maxPatrolPointDistance, bossExclusionRadius) ?? GetClosestPatrolPoint(maxPatrolPointDistance);
                if (closestPoint == null)
                {
                    continue;
                }

                float distance = Vector3.Distance(closestPoint.Position, _bot.Position);
                if (distance > maxPatrolPointDistance)
                {
                    continue;
                }

                if (distance < closestPointDistance)
                {
                    closestPointDistance = distance;
                    _selectedPoint = closestPoint;
                    closestWay = way;
                }
            }

            return closestWay;
        }

        private PatrolPoint? GetClosestPatrolPoint(float maxDistance)
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

                float distance = Vector3.Distance(patrolPoint.Position, _bot.Position);
                if (distance > maxDistance)
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

        private PatrolPoint? GetClosestPatrolPointNearBoss(float maxDistance, float exclusionRadiusAroundBoss)
        {
            if (!_bot.BotFollower.HaveBoss || !_bot.BotFollower.BossToFollow.IsAlive)
            {
                return null;
            }

            float closestPointDistance = float.MaxValue;
            PatrolPoint? closestPoint = null;
            foreach (PatrolPoint patrolPoint in _bot.PatrollingData.PointControl.Way.Points)
            {
                if (!patrolPoint.IsFreeFor(_bot))
                {
                    //Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " cannot use patrol point at " + patrolPoint.Position + "; reserved for " + patrolPoint.Owner.GetText());
                    continue;
                }

                float distance = Vector3.Distance(patrolPoint.Position, _bot.BotFollower.BossToFollow.Position);
                if (distance > maxDistance)
                {
                    continue;
                }

                if (distance <= exclusionRadiusAroundBoss)
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
