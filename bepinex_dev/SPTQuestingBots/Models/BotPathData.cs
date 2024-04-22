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
