using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class BotPathingHelpers
    {
        private static FieldInfo pathPointsField = AccessTools.Field(typeof(GClass529), "vector3_0");
        private static FieldInfo pathIndexField = AccessTools.Field(typeof(GClass529), "int_0");

        public static void FollowPath(this BotOwner bot, Models.Pathing.BotPathData botPath, bool slowAtTheEnd, bool getUpWithCheck)
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
            PathControllerClass pathController = botMover.GetPathController();
            if (pathController?.CurPath == null)
            {
                return null;
            }

            Vector3[] path = (Vector3[])pathPointsField.GetValue(pathController.CurPath);

            return path;
        }

        public static bool HasSameTargetPosition(this BotOwner bot, Vector3 targetPosition)
        {
            PathControllerClass pathController = bot?.Mover.GetPathController();
            if (pathController?.CurPath == null)
            {
                return false;
            }

            return pathController.IsSameWay(targetPosition, bot.Position);
        }

        public static PathControllerClass GetPathController(this BotMover botMover)
        {
            if (botMover == null)
            {
                return null;
            }

            if (botMover._pathController?.CurPath == null)
            {
                return null;
            }

            return botMover._pathController;
        }

        public static bool SetSlowAtTheEnd(this BotMover botMover, bool slowAtTheEnd)
        {
            if (botMover == null)
            {
                return false;
            }

            if (botMover._pathFinder == null)
            {
                return false;
            }

            botMover._pathFinder.SlowAtTheEnd = slowAtTheEnd;
            return true;
        }

        public static int? GetCurrentCornerIndex(this BotMover botMover)
        {
            if (botMover == null)
            {
                return null;
            }

            if (botMover._pathController?.CurPath == null)
            {
                return null;
            }

            int index = (int)pathIndexField.GetValue(botMover._pathController.CurPath);

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

            if (botMover._pathController?.CurPath == null)
            {
                return null;
            }

            return botMover._pathController.CurPath?.TargetPoint?.Position;
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
