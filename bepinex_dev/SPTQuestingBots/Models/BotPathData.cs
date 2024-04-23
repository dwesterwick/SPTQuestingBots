using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public enum BotPathUpdateReason
    {
        None = 0,
        Force,
        RefreshNeededTime,
        RefreshNeededPath,
        NewTarget,
        IncompletePath,
    }

    public class BotPathData : StaticPathData
    {
        public static float MinRefreshDistance { get; } = 0.5f;
        public static float IncompletePathRefreshDelay { get; } = 5f;
        public float DistanceToTarget => Vector3.Distance(bot.Position, TargetPosition);

        private BotOwner bot;
        
        public BotPathData(BotOwner botOwner) : base()
        {
            bot = botOwner;
        }

        public BotPathUpdateReason Update(Vector3 target, float reachDistance = 0.5f, bool force = false)
        {
            bool requiresUpdate = false;
            BotPathUpdateReason reason = BotPathUpdateReason.None;

            float distanceFromStartPosition = Vector3.Distance(bot.Position, StartPosition);

            if (force)
            {
                requiresUpdate = true;
                reason = BotPathUpdateReason.Force;
            }

            if (!requiresUpdate && ((target != TargetPosition) || (reachDistance != ReachDistance)))
            {
                TargetPosition = target;
                ReachDistance = reachDistance;

                requiresUpdate = true;
                reason = BotPathUpdateReason.NewTarget;
            }

            if (!requiresUpdate)
            {
                if (Status == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    requiresUpdate = true;
                    reason = BotPathUpdateReason.IncompletePath;
                }
                else if ((Status == UnityEngine.AI.NavMeshPathStatus.PathPartial) && (Time.time - LastSetTime > IncompletePathRefreshDelay))
                {
                    requiresUpdate = true;
                    reason = BotPathUpdateReason.IncompletePath;
                }
            }

            /*if (!requiresUpdate && (bot.Mover.LastPathSetTime != LastSetTime))
            {
                requiresUpdate &= distanceFromStartPosition > MinRefreshDistance;
                reason = BotPathUpdateReason.RefreshNeededTime;
            }*/

            if (!requiresUpdate)
            {
                Vector3[] currentPath = bot.Mover.GetCurrentPath();
                if (Corners.Any() && (currentPath?.Any() == true) && (currentPath.Last() != Corners.Last()))
                {
                    requiresUpdate &= distanceFromStartPosition > MinRefreshDistance;
                    reason = BotPathUpdateReason.RefreshNeededPath;
                }
            }

            if (requiresUpdate)
            {
                updateCorners(target);
            }

            return reason;
        }

        public float GetDistanceToFinalPoint()
        {
            if (Corners.Length == 0)
            {
                return float.NaN;
            }

            return Vector3.Distance(bot.Position, Corners.Last());
        }

        private void updateCorners(Vector3 target)
        {
            StartPosition = bot.Position;

            Status = CreatePathSegment(bot.Position, target, out Vector3[] corners);
            if (Status == UnityEngine.AI.NavMeshPathStatus.PathPartial)
            {
                BotQuestBuilder botQuestBuilder = Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>();
                IEnumerable<StaticPathData> staticPaths = botQuestBuilder
                    .GetStaticPaths(target)
                    .OrderBy(p => p.PathLength + Vector3.Distance(bot.Position, p.StartPosition));

                foreach (StaticPathData staticPath in staticPaths)
                {
                    UnityEngine.AI.NavMeshPathStatus staticPathStatus = CreatePathSegment(bot.Position, staticPath.StartPosition, out Vector3[] staticPathCorners);
                    if (staticPathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    {
                        Corners = staticPathCorners;
                        Status = UnityEngine.AI.NavMeshPathStatus.PathComplete;

                        StaticPathData combinedPath = Append(staticPath);
                        SetCorners(combinedPath.Corners);

                        LoggingController.LogInfo("Using static path from " + staticPath.StartPosition + " to " + staticPath.TargetPosition + " for " + bot.GetText());

                        return;
                    }
                }
            }

            SetCorners(corners);
        }
    }
}
