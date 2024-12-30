using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class BotPathingHelpers
    {
        private static FieldInfo pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        private static FieldInfo pathFinderField = AccessTools.Field(typeof(BotMover), "_pathFinder");
        private static FieldInfo pathPointsField = AccessTools.Field(typeof(GClass513), "vector3_0");
        private static FieldInfo pathIndexField = AccessTools.Field(typeof(GClass513), "int_0");

        public static void FollowPath(this BotOwner bot, BotPathData botPath, bool slowAtTheEnd, bool getUpWithCheck)
        {
            // Combines _pathFinder.method_0 and GoToByWay

            //LoggingController.LogInfo("Updated path for " + bot.GetText());

            bot.Mover?.SetSlowAtTheEnd(slowAtTheEnd);

            if (bot.BotLay.IsLay && (botPath.DistanceToTarget > 0.2f))
            {
                bot.BotLay.GetUp(getUpWithCheck);
            }

            bot.WeaponManager.Stationary.StartMove();

            bot.Mover?.GoToByWay(botPath.Corners, botPath.ReachDistance);
        }

        public static Vector3[] GetCurrentPath(this BotMover botMover)
        {
            if (botMover == null)
            {
                return null;
            }

            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            if (pathController?.CurPath == null)
            {
                return null;
            }

            Vector3[] path = (Vector3[])pathPointsField.GetValue(pathController.CurPath);

            return path;
        }

        public static bool HasSameTargetPosition(this BotOwner bot, Vector3 targetPosition)
        {
            if (bot?.Mover == null)
            {
                return false;
            }

            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(bot.Mover);
            if (pathController?.CurPath == null)
            {
                return false;
            }

            return pathController.IsSameWay(targetPosition, bot.Position);
        }

        public static bool SetSlowAtTheEnd(this BotMover botMover, bool slowAtTheEnd)
        {
            if (botMover == null)
            {
                return false;
            }

            GClass470 pathFinder = (GClass470)pathFinderField.GetValue(botMover);
            if (pathFinder == null)
            {
                return false;
            }

            pathFinder.SlowAtTheEnd = slowAtTheEnd;
            return true;
        }

        public static int? GetCurrentCornerIndex(this BotMover botMover)
        {
            if (botMover == null)
            {
                return null;
            }

            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            if (pathController?.CurPath == null)
            {
                return null;
            }

            int index = (int)pathIndexField.GetValue(pathController.CurPath);

            return index;
        }

        public static Vector3? GetCurrentPathLastPoint(this BotMover botMover)
        {
            Vector3[] currentPath = botMover.GetCurrentPath();
            Vector3? lastPathPoint = currentPath?.Last();

            return lastPathPoint;
        }

        public static Vector3? GetCurrentPathTargetPoint(this BotMover botMover)
        {
            if (botMover == null)
            {
                return null;
            }

            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            if (pathController?.CurPath == null)
            {
                return null;
            }

            return pathController.CurPath?.TargetPoint?.Position;
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
