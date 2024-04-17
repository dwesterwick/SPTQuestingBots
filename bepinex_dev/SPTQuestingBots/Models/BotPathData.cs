using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Models
{
    public class BotPathData
    {
        public Vector3 TargetPosition { get; private set; } = Vector3.positiveInfinity;
        public NavMeshPathStatus Status { get; private set; } = NavMeshPathStatus.PathInvalid;
        public float ReachDistance { get; private set; } = float.NaN;
        public Vector3[] Corners { get; private set; } = new Vector3[0];

        public float DistanceToTarget => Vector3.Distance(bot.Position, TargetPosition);

        private BotOwner bot;

        public BotPathData(BotOwner botOwner)
        {
            bot = botOwner;
        }

        public bool Update(Vector3 target, float reachDistance = 0.5f, bool force = false)
        {
            bool requiresUpdate = force;

            if (!bot.HasSameTargetPosition(target))
            {
                LoggingController.LogInfo((bot.Mover.TargetPoint?.ToString() ?? "???") + " != " + target.ToString());
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
                Status = createPathSegment(bot.Position, target, out Vector3[] corners);
                Corners = corners;
            }

            return requiresUpdate;
        }

        public bool IsComplete()
        {
            if (Corners.Length == 0)
            {
                return false;
            }

            if (Vector3.Distance(TargetPosition, Corners.Last()) > ReachDistance)
            {
                return false;
            }

            return true;
        }

        public float GetDistanceToFinalPoint()
        {
            if (Corners.Length == 0)
            {
                return float.NaN;
            }

            return Vector3.Distance(bot.Position, Corners.Last());
        }

        public float GetMissingDistanceToTarget()
        {
            if (Corners.Length == 0)
            {
                return float.NaN;
            }

            return Vector3.Distance(TargetPosition, Corners.Last());
        }

        private NavMeshPathStatus createPathSegment(Vector3 start, Vector3 end, out Vector3[] pathCorners)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(start, end, -1, navMeshPath);
            pathCorners = navMeshPath.corners;

            return navMeshPath.status;
        }
    }
}
