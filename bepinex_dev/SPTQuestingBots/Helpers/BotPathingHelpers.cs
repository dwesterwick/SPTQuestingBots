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
            /*if (bot.DoorOpener.Interacting)
            {
                LoggingController.LogWarning("Cannot update path for " + bot.GetText() + " while it is interacting with a door");
                return;
            }*/

            // Combines _pathFinder.method_0 and GoToByWay

            //LoggingController.LogInfo("Updated path for " + bot.GetText());

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
            if (botMover?._pathController?.CurPath == null)
            {
                return Array.Empty<Vector3>();
            }

            Vector3[] path = (Vector3[])pathPointsField.GetValue(botMover._pathController.CurPath);

            return path;
        }

        public static bool HasSamePathTargetPosition(this BotOwner bot, Vector3 targetPosition)
        {
            if (bot?.Mover?._pathController == null)
            {
                return false;
            }

            return bot.Mover._pathController.IsSameWay(targetPosition, bot.Position);
        }

        public static bool TrySetSlowAtTheEnd(this BotMover botMover, bool slowAtTheEnd)
        {
            if (botMover?._pathFinder == null)
            {
                return false;
            }

            botMover._pathFinder.SlowAtTheEnd = slowAtTheEnd;
            return true;
        }

        public static Vector3? GetCurrentPathCornerPosition(this BotMover botMover)
        {
            if (botMover?._pathController?.CurPath == null)
            {
                return null;
            }

            return botMover._pathController.CurPath.CurrentCorner();
        }

        public static int? GetCurrentPathCornerIndex(this BotMover botMover)
        {
            if (botMover?._pathController?.CurPath == null)
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
            if (botMover?._pathController?.CurPath == null)
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
