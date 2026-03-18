using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.Components;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using UnityEngine;

namespace QuestingBots.Models.Pathing
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

        public void ClearPath()
        {
            SetCorners(new Vector3[0]);
        }

        public void ForcePathRecalculation()
        {
            Status = UnityEngine.AI.NavMeshPathStatus.PathPartial;
        }

        public BotPathUpdateNeededReason CheckIfUpdateIsNeeded(Vector3 target, float targetVariationAllowed = 0.2f, float reachDistance = 0.5f, bool force = false)
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
            float targetVariation = Vector3.Distance(target, TargetPosition);
            if (!requiresUpdate && ((targetVariation > targetVariationAllowed) || (reachDistance != ReachDistance)))
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
                    Vector3? navMeshPosition = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().FindNearestNavMeshPosition(bot.Position, 2);
                    if (!navMeshPosition.HasValue)
                    {
                        Singleton<LoggingUtil>.Instance.LogError("Cannot find NavMesh position for " + bot.GetText());
                    }
                    else
                    {
                        float distance = Vector3.Distance(bot.Position, navMeshPosition.Value);
                        Singleton<LoggingUtil>.Instance.LogError(bot.GetText() + " has an invalid path and is " + distance + "m from the NavMesh");

                        if (distance > 0.05)
                        {
                            Singleton<LoggingUtil>.Instance.LogError("Teleporting " + bot.GetText() + " to nearest NavMesh position...");
                            bot.GetPlayer.Teleport(navMeshPosition.Value);
                        }
                    }

                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.IncompletePath;
                }
                else if ((Status == UnityEngine.AI.NavMeshPathStatus.PathPartial) && (Time.time - LastSetTime > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.IncompletePathRetryInterval))
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

                if (!requiresUpdate && !Corners.Any())
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " has no cached corners. Updating path...");
                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.RefreshNeededPath;
                }

                if (!requiresUpdate && Corners.Any() && !currentPath.Any())
                {
                    //Singleton<LoggingUtil>.Instance.LogInfo(bot.GetText() + " has no path set in EFT but has cached corners. Updating path...");
                    requiresUpdate = true;
                    reason = BotPathUpdateNeededReason.RefreshNeededPath;
                }

                if (!requiresUpdate && Corners.Any() && currentPath.Any() && (currentPath.Last() != Corners.Last()))
                {
                    // Only update the path if the bot has moved from the start position set in the currently cached path. Otherwise, the path may
                    // constantly be recalculated as brain layers are switched. 
                    requiresUpdate &= distanceFromStartPosition > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotPathing.MaxStartPositionDiscrepancy;
                    reason = BotPathUpdateNeededReason.RefreshNeededPath;
                }
            }

            if (requiresUpdate)
            {
                updateCorners(target, reason == BotPathUpdateNeededReason.IncompletePath);
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

        private void updateCorners(Vector3 target, bool ignoreDuplicates = false)
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
                    Singleton<LoggingUtil>.Instance.LogInfo("Testing " + staticPaths.Count() + " static paths for " + bot.GetText() + "...");
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

                        Singleton<LoggingUtil>.Instance.LogInfo("Using static path from " + staticPath.StartPosition + " to " + staticPath.TargetPosition + " for " + bot.GetText());
                        //Singleton<LoggingUtil>.Instance.LogInfo("Path to Static Path: " + string.Join(", ", staticPathCorners));
                        //Singleton<LoggingUtil>.Instance.LogInfo("Static Path: " + string.Join(", ", staticPath.Corners));
                        //Singleton<LoggingUtil>.Instance.LogInfo("Combined Path: " + string.Join(", ", Corners));

                        return;
                    }
                }
            }

            SetCorners(corners);
        }
    }
}
