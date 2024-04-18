using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class StaticPathData
    {
        public Vector3 StartPosition { get; protected set; } = Vector3.negativeInfinity;
        public Vector3 TargetPosition { get; protected set; } = Vector3.positiveInfinity;
        public NavMeshPathStatus Status { get; protected set; } = NavMeshPathStatus.PathInvalid;
        public float ReachDistance { get; protected set; } = float.NaN;
        public Vector3[] Corners { get; protected set; } = new Vector3[0];
        public float LastSetTime { get; protected set; } = 0;

        public StaticPathData()
        {

        }

        public StaticPathData(Vector3 start, Vector3 target, float reachDistance)
        {
            StartPosition = start;
            TargetPosition = target;
            ReachDistance = reachDistance;

            Status = CreatePathSegment(start, target, out Vector3[] corners);
            Corners = corners;

            LastSetTime = Time.time;
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

        public float GetMissingDistanceToTarget()
        {
            if (Corners.Length == 0)
            {
                return float.NaN;
            }

            return Vector3.Distance(TargetPosition, Corners.Last());
        }

        protected NavMeshPathStatus CreatePathSegment(Vector3 start, Vector3 end, out Vector3[] pathCorners)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(start, end, -1, navMeshPath);
            pathCorners = navMeshPath.corners;

            return navMeshPath.status;
        }
    }
}
