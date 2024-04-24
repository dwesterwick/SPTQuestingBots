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
    public enum BotPathUpdateNeededReason
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
        public float DistanceToTarget => Vector3.Distance(bot.Position, TargetPosition);

        private BotOwner bot;
        
        public BotPathData(BotOwner botOwner) : base()
        {
            bot = botOwner;
        }

        public BotPathUpdateNeededReason CheckIfUpdateIsNeeded(Vector3 target, float reachDistance = 0.5f, bool force = false)
        {
            bool requiresUpdate = false;
            BotPathUpdateNeededReason reason = BotPathUpdateNeededReason.None;

            float distanceFromStartPosition = Vector3.Distance(bot.Position, StartPosition);

            if (force)
            {
                requiresUpdate = true;
                reason = BotPathUpdateNeededReason.Force;
            }

            // Check if a new target position has been set or if the reach distance has been modified
            if (!requiresUpdate && ((target != TargetPosition) || (reachDistance != ReachDistance)))
            {
                TargetPosition = target;
                ReachDistance = reachDistance;

                requiresUpdate = true;
                reason = BotPathUpdateNeededReason.NewTarget;
            }

            // If the path is incomplete, it should be regularly updated in case Unity is able to resolve the it
            if (!requiresUpdate)
            {
                if (Status == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
                {
                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.IncompletePath;
                }
                else if ((Status == UnityEngine.AI.NavMeshPathStatus.PathPartial) && (Time.time - LastSetTime > ConfigController.Config.Questing.BotPathing.IncompletePathRetryInterval))
                {
                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.IncompletePath;
                }
            }

            // Check if the bot's path has been changed by another component (i.e. Looting Bots, SAIN, etc.)
            if (!requiresUpdate)
            {
                Vector3[] currentPath = bot.Mover.GetCurrentPath();
                if (currentPath == null)
                {
                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.RefreshNeededPath;
                }

                if (!requiresUpdate && Corners.Any() && (currentPath?.Any() == true) && (currentPath.Last() != Corners.Last()))
                {
                    // Only update the path if the bot has moved from the start position set in the currently cached path. Otherwise, the path may
                    // constantly be recalculated as brain layers are switched. 
                    requiresUpdate &= distanceFromStartPosition > ConfigController.Config.Questing.BotPathing.MaxStartPositionDiscrepancy;
                    reason = BotPathUpdateNeededReason.RefreshNeededPath;
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
                // Check if any static paths exist to the target position and sort them based on the approximate total path length for the bot
                BotQuestBuilder botQuestBuilder = Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>();
                IEnumerable<StaticPathData> staticPaths = botQuestBuilder
                    .GetStaticPaths(target)
                    .OrderBy(p => p.PathLength + Vector3.Distance(bot.Position, p.StartPosition));

                /*if (staticPaths.Any())
                {
                    LoggingController.LogInfo("Testing " + staticPaths.Count() + " static paths for " + bot.GetText() + "...");
                }*/

                foreach (StaticPathData staticPath in staticPaths)
                {
                    // Check if Unity can form a complete path from the bot to the static path's endpoint
                    UnityEngine.AI.NavMeshPathStatus staticPathStatus = CreatePathSegment(bot.Position, staticPath.StartPosition, out Vector3[] staticPathCorners);
                    if (staticPathStatus == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                    {
                        Corners = staticPathCorners;
                        Status = UnityEngine.AI.NavMeshPathStatus.PathComplete;

                        // Merge the paths and instruct the bot to use the combination
                        StaticPathData combinedPath = Append(staticPath);
                        SetCorners(combinedPath.Corners);

                        LoggingController.LogInfo("Using static path from " + staticPath.StartPosition + " to " + staticPath.TargetPosition + " for " + bot.GetText());
                        //LoggingController.LogInfo("Path to Static Path: " + string.Join(", ", staticPathCorners));
                        //LoggingController.LogInfo("Static Path: " + string.Join(", ", staticPath.Corners));
                        //LoggingController.LogInfo("Combined Path: " + string.Join(", ", Corners));

                        return;
                    }
                }
            }

            SetCorners(corners);
        }
    }
}
