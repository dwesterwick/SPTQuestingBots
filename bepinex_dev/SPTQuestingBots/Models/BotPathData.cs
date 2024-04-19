using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotPathData : StaticPathData
    {
        public float DistanceToTarget => Vector3.Distance(bot.Position, TargetPosition);

        private BotOwner bot;
        
        public BotPathData(BotOwner botOwner) : base()
        {
            bot = botOwner;
        }

        public bool Update(Vector3 target, float reachDistance = 0.5f, bool force = false)
        {
            bool requiresUpdate = force;

            if (bot.Mover.LastPathSetTime != LastSetTime)
            {
                requiresUpdate = true;
            }

            if ((target != TargetPosition) || (reachDistance != ReachDistance))
            {
                TargetPosition = target;
                ReachDistance = reachDistance;
                requiresUpdate = true;
            }

            requiresUpdate |= Corners.Length == 0;

            if (requiresUpdate)
            {
                updateCorners(target);
            }

            return requiresUpdate;
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
            Corners = corners;

            /*if (Status == UnityEngine.AI.NavMeshPathStatus.PathPartial)
            {
                IEnumerable<Vector3> assembledPath = assemblePartialPaths(target).ToArray();

                if (assembledPath.Any())
                {
                    Corners = assembledPath.ToArray();
                }
            }*/

            LastSetTime = Time.time;
        }

        private IEnumerable<Vector3> assemblePartialPaths(Vector3 target)
        {
            List<Vector3> corners = new List<Vector3>();

            Vector3 segmentTarget = target;
            Vector3 nextCorner = Vector3.negativeInfinity;
            StaticPathData segment = null;
            do
            {
                segment = getPreviousSegment(segmentTarget, nextCorner);
                if (segment != null)
                {
                    LoggingController.LogInfo("Inserting path from " + segment.StartPosition + " to " + segment.TargetPosition + " for " + bot.GetText());

                    corners.InsertRange(0, segment.Corners);
                    nextCorner = corners[0];

                    if (segment.StartPosition == bot.Position)
                    {
                        Status = UnityEngine.AI.NavMeshPathStatus.PathComplete;
                        return corners;
                    }

                    segmentTarget = segment.StartPosition;
                }
            } while (segment != null);

            return Enumerable.Empty<Vector3>();
        }

        private StaticPathData getPreviousSegment(Vector3 target, Vector3 nextCorner)
        {
            BotQuestBuilder botQuestBuilder = Singleton<GameWorld>.Instance.GetComponent<BotQuestBuilder>();
            IEnumerable<StaticPathData> staticPaths = botQuestBuilder
                .GetStaticPaths(target)
                .Where(p => p.Corners[0] != nextCorner);
            
            if (!staticPaths.Any())
            {
                return null;
            }

            LoggingController.LogInfo("Found " + staticPaths.Count() + " possible paths to " + target + " for " + bot.GetText());

            IEnumerable<StaticPathData> orderedPaths = staticPaths.OrderByDescending(p => Vector3.Distance(p.StartPosition, target));
            foreach (StaticPathData pathData in orderedPaths)
            {
                Status = CreatePathSegment(bot.Position, pathData.StartPosition, out Vector3[] segmentCorners);
                if (Status == UnityEngine.AI.NavMeshPathStatus.PathComplete)
                {
                    return pathData;
                }
            }

            return orderedPaths.Last();
        }
    }
}
