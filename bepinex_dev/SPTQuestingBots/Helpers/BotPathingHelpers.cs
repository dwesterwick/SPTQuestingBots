using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT.UI;
using HarmonyLib;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class BotPathingHelpers
    {
        private static FieldInfo pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        private static FieldInfo pathPointsField = AccessTools.Field(typeof(GClass466), "vector3_0");
        private static FieldInfo pathIndexField = AccessTools.Field(typeof(GClass466), "int_0");

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
    }
}
