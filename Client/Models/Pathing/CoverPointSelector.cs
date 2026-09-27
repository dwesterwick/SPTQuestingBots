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
    public class CoverPointSelector
    {
        private BotOwner _bot;

        private CustomNavigationPoint? _coverPoint = null;

        private float MaxDistance => 25;

        public Vector3? CoverPoint => _coverPoint?.Position;
        public bool HasCoverPoint => _coverPoint != null;

        public CoverPointSelector(BotOwner bot)
        {
            _bot = bot;
        }

        public void RefreshCoverPoint()
        {
            if ((CoverPoint != null) && (Vector3.Distance(_bot.Position, CoverPoint.Value) < MaxDistance))
            {
                Singleton<LoggingUtil>.Instance.LogDebug(_bot.GetText() + " already has a nearby cover point");
                return;
            }

            _coverPoint = null;

            CoverSearchDefenceData coverSearchDefenceData = new CoverSearchDefenceData(_bot.Settings.FileSettings.Cover.MIN_DEFENCE_LEVEL);
            Vector3? closestFriendCoverPoint = _bot.Covers.ClosestFriendCoverPoint();

            CoverSearchData coverSearchData = new CoverSearchData(_bot.Position, _bot.CoverSearchInfo, CoverShootType.hide, MaxDistance * MaxDistance, 0, CoverSearchType.distToBotAndToCenter, _bot.CurrentEnemyTargetPosition(true), closestFriendCoverPoint, null, ECheckSHootHide.shootAndHide, coverSearchDefenceData, PointsArrayType.allWithBush);
            
            CustomNavigationPoint newCoverPoint = _bot.BotsGroup.CoverPointMaster.GetCoverPointMain(coverSearchData, true);
            if (newCoverPoint == null)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Could not find a new cover point for " + _bot.GetText());
                return;
            }

            float distance = Vector3.Distance(_bot.Position, newCoverPoint.Position);
            if (distance > MaxDistance)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("New cover point for " + _bot.GetText() + " is too far (" + distance + "m)");
                return;
            }

            Singleton<LoggingUtil>.Instance.LogDebug("Found cover point for " + _bot.GetText());
            _coverPoint = newCoverPoint;
        }
    }
}
