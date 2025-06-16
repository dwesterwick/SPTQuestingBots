using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using HarmonyLib;

namespace SPTQuestingBots.Models.Pathing
{
    public class StaticPathData : ICloneable
    {
        public Vector3 StartPosition { get; protected set; } = Vector3.negativeInfinity;
        public Vector3 TargetPosition { get; protected set; } = Vector3.positiveInfinity;
        public NavMeshPathStatus Status { get; protected set; } = NavMeshPathStatus.PathInvalid;
        public float ReachDistance { get; protected set; } = float.NaN;
        public Vector3[] Corners { get; protected set; } = new Vector3[0];
        public float PathLength { get; protected set; } = float.NaN;
        public float LastSetTime { get; protected set; } = 0;

        public bool IsInitialized => TargetPosition != Vector3.positiveInfinity;
        public bool HasPath => Corners.Length > 0;
        public float TimeSinceLastSet => Time.time - LastSetTime;

        public StaticPathData()
        {

        }

        public StaticPathData(Vector3 start, Vector3 target, float reachDistance)
        {
            StartPosition = start;
            TargetPosition = target;
            ReachDistance = reachDistance;

            Status = CreatePathSegment(start, target, out Vector3[] corners);
            SetCorners(corners);
        }

        public object Clone()
        {
            StaticPathData clone = new StaticPathData();
            clone.StartPosition = StartPosition;
            clone.TargetPosition = TargetPosition;
            clone.Status = Status;
            clone.ReachDistance = ReachDistance;
            clone.Corners = Corners;
            clone.PathLength = PathLength;
            clone.LastSetTime = LastSetTime;

            return clone;
        }

        public StaticPathData GetReverse()
        {
            StaticPathData reverse = new StaticPathData();
            reverse.StartPosition = TargetPosition;
            reverse.TargetPosition = StartPosition;
            reverse.Status = Status;
            reverse.ReachDistance = ReachDistance;
            reverse.Corners = Corners.Reverse().ToArray();
            reverse.PathLength = PathLength;
            reverse.LastSetTime = LastSetTime;

            return reverse;
        }

        public StaticPathData Append(StaticPathData pathToAppend)
        {
            if (pathToAppend.Corners.Length == 0)
            {
                return this;
            }

            StaticPathData newPath = (StaticPathData)Clone();

            Vector3[] newCorners;
            if (Corners.Last() == pathToAppend.Corners.First())
            {
                newCorners = Corners.AddRangeToArray(pathToAppend.Corners.Skip(1).ToArray());
            }
            else
            {
                newCorners = Corners.AddRangeToArray(pathToAppend.Corners);
            }

            newPath.TargetPosition = pathToAppend.TargetPosition;
            newPath.CombineWithPath(pathToAppend, newCorners);

            return newPath;
        }

        public StaticPathData Prepend(StaticPathData pathToPrepend)
        {
            if (pathToPrepend.Corners.Length == 0)
            {
                return this;
            }

            StaticPathData newPath = (StaticPathData)Clone();

            Vector3[] newCorners;
            if (Corners.First() == pathToPrepend.Corners.Last())
            {
                newCorners = pathToPrepend.Corners.AddRangeToArray(Corners.Skip(1).ToArray());
            }
            else
            {
                newCorners = pathToPrepend.Corners.AddRangeToArray(Corners);
            }

            newPath.StartPosition = pathToPrepend.StartPosition;
            newPath.CombineWithPath(pathToPrepend, newCorners);

            return newPath;
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

        protected void CombineWithPath(StaticPathData pathToMerge, Vector3[] combinedCorners)
        {
            if (pathToMerge.ReachDistance < ReachDistance)
            {
                ReachDistance = pathToMerge.ReachDistance;
            }

            if (pathToMerge.Status == NavMeshPathStatus.PathInvalid)
            {
                Status = NavMeshPathStatus.PathInvalid;
            }
            else if (pathToMerge.Status == NavMeshPathStatus.PathPartial)
            {
                Status = NavMeshPathStatus.PathPartial;
            }

            SetCorners(combinedCorners);
        }

        protected void SetCorners(Vector3[] corners)
        {
            Corners = corners;
            PathLength = Corners.CalculatePathLength();
            LastSetTime = Time.time;
        }
    }
}
