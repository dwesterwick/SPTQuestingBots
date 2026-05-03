using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Helpers
{
    public static class BotPathingHelpers
    {
        private static FieldInfo pathPointsField = AccessTools.Field(typeof(BotCurrentPathAbstractClass), "Vector3_0");
        private static FieldInfo pathIndexField = AccessTools.Field(typeof(BotCurrentPathAbstractClass), "Int_0");

        public static NavMeshObstacle GetOrAddNavMeshObstacle(this GameObject gameObject)
        {
            NavMeshObstacle navMeshObstacle = gameObject.GetOrAddComponent<NavMeshObstacle>();
            navMeshObstacle.shape = NavMeshObstacleShape.Box;
            navMeshObstacle.center = Vector3.zero;
            navMeshObstacle.size = Vector3.one;
            navMeshObstacle.carving = true;

            return navMeshObstacle;
        }

        public static NavMeshPathStatus CreatePathSegment(Vector3 start, Vector3 end, out Vector3[] pathCorners)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            NavMesh.CalculatePath(start, end, -1, navMeshPath);
            pathCorners = navMeshPath.corners;

            return navMeshPath.status;
        }

        public static void FollowPath(this BotOwner bot, Models.Pathing.BotPathData botPath, bool slowAtTheEnd, bool getUpWithCheck)
        {
            /*if (bot.DoorOpener.Interacting)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Cannot update path for " + bot.GetText() + " while it is interacting with a door");
                return;
            }*/

            // Combines _pathFinder.method_0 and GoToByWay

            //Singleton<LoggingUtil>.Instance.LogInfo("Updated path for " + bot.GetText());

            bot.Mover?.TrySetSlowAtTheEnd(slowAtTheEnd);

            if (bot.BotLay.IsLay && (botPath.DistanceToTarget > 0.2f))
            {
                bot.BotLay.GetUp(getUpWithCheck);
            }

            bot.WeaponManager.Stationary.StartMove();

            bot.Mover?.GoToByWay(botPath.Corners, botPath.ReachDistance);
        }

        public static Vector3[] GetCurrentPath(this BotMover botMover)
        {
            if (botMover?.ActualPathController?.CurPath == null)
            {
                return Array.Empty<Vector3>();
            }

            Vector3[] path = (Vector3[])pathPointsField.GetValue(botMover.ActualPathController.CurPath);

            return path;
        }

        public static bool HasSamePathTargetPosition(this BotOwner bot, Vector3 targetPosition)
        {
            if (bot?.Mover?.ActualPathController == null)
            {
                return false;
            }

            return bot.Mover.ActualPathController.IsSameWay(targetPosition, bot.Position);
        }

        public static bool TrySetSlowAtTheEnd(this BotMover botMover, bool slowAtTheEnd)
        {
            if (botMover?.PathFinder == null)
            {
                return false;
            }

            botMover.PathFinder.SlowAtTheEnd = slowAtTheEnd;
            return true;
        }

        public static Vector3? GetCurrentPathCornerPosition(this BotMover botMover)
        {
            if (botMover?.ActualPathController?.CurPath == null)
            {
                return null;
            }

            return botMover.ActualPathController.CurPath.CurrentCorner();
        }

        public static int? GetCurrentPathCornerIndex(this BotMover botMover)
        {
            if (botMover?.ActualPathController?.CurPath == null)
            {
                return null;
            }

            int index = (int)pathIndexField.GetValue(botMover.ActualPathController.CurPath);

            return index;
        }

        public static Vector3? GetCurrentPathLastPoint(this BotMover botMover)
        {
            Vector3[] currentPath = botMover.GetCurrentPath();
            if ((currentPath == null) || (currentPath.Length == 0))
            {
                return null;
            }

            Vector3 lastPathPoint = currentPath.Last();
            return lastPathPoint;
        }

        public static Vector3? GetCurrentPathTargetPoint(this BotMover botMover)
        {
            if (botMover?.ActualPathController?.CurPath == null)
            {
                return null;
            }

            return botMover.ActualPathController.CurPath?.TargetPoint?.Position;
        }

        public static bool IsPathComplete(this BotMover botMover, Vector3 destination, float maxDistanceError)
        {
            Vector3? lastPoint = botMover.GetCurrentPathLastPoint();
            if (!lastPoint.HasValue)
            {
                return false;
            }

            if (Vector3.Distance(destination, lastPoint.Value) > maxDistanceError)
            {
                return false;
            }

            return true;
        }

        public static bool IsSamePath(this IEnumerable<Vector3> path, IEnumerable<Vector3> other)
        {
            if (path.Count() != other.Count())
            {
                return false;
            }

            IEnumerable<bool> areElementsEqual = path.Zip(other, (first, second) => first == second);

            return areElementsEqual.All(e => e == true);
        }
    }
}
