using Comfort.Common;
using EFT;
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
        protected override float MaxSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MaxSearchDistanceForBosses;
        protected override float MaxSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MaxSearchDistanceForFollowers;
        protected override float MinSearchDistanceBoss => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MinSearchDistanceForBosses;
        protected override float MinSearchDistanceFollower => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.MinSearchDistanceForFollowers;
        protected override float DebounceTimeAfterChecking => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.DebounceTimeAfterChecking;
        protected override float DebounceTimeAfterUpdating => Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotZoneUpdates.CoverPointUpdates.DebounceTimeAfterUpdating;

        public CoverLevel SelectedPointCoverLevel => SelectedPoint?.CoverLevel ?? CoverLevel.Stay;
        public Vector3 SelectedPointToWallVector => SelectedPoint?.ToWallVector ?? Vector3.zero;
        public bool IsAtSelectedPoint => DistanceToSelectedPoint <= 0.5f;

        public CoverPointSelector(BotOwner bot) : base(bot)
        {

        }

        public void ReserveSelectedCoverPoint()
        {
            Bot.Memory.SetCoverPoints(SelectedPoint);
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
            return HasAQuestingBoss() ? Bot.BotFollower.BossToFollow.Position : Bot.Position;
        }

        protected override void Refresh_Internal(Vector3 centerPoint)
        {
            float maxSearchDistance = GetMaxSearchDistance();

            if ((SelectedPosition != null) && (Vector3.Distance(centerPoint, SelectedPosition.Value) < maxSearchDistance))
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
            if (distance > maxSearchDistance)
            {
                //Singleton<LoggingUtil>.Instance.LogDebug("New cover point for " + _bot.GetText() + " is too far (" + distance + "m)");
                return;
            }

            if (!Bot.Position.HasCompletePathTo(newCoverPoint.Position))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(Bot.GetText() + " does not have a complete path to new cover point " + distance + "m away");
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Found cover point for " + Bot.GetText());
            SetSelectedPoint(newCoverPoint);

            ReserveSelectedCoverPoint();
        }

        private CustomNavigationPoint? GetNewCoverPoint(Vector3 centerPoint)
        {
            CoverSearchDefenceData coverSearchDefenceData = new CoverSearchDefenceData(Bot.Settings.FileSettings.Cover.MIN_DEFENCE_LEVEL);
            Vector3? closestFriendCoverPoint = Bot.Covers.ClosestFriendCoverPoint();

            CoverSearchData coverSearchData = new CoverSearchData(centerPoint, Bot.CoverSearchInfo, CoverShootType.hide, GetMaxSearchDistanceSqr(),
                GetMinSearchDistanceSqr(), CoverSearchType.distToBotAndToCenter, Bot.CurrentEnemyTargetPosition(true), closestFriendCoverPoint, null,
                ECheckSHootHide.shootAndHide, coverSearchDefenceData, PointsArrayType.allWithBush);

            CustomNavigationPoint newCoverPoint = Bot.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);

            return newCoverPoint;
        }
    }
}
