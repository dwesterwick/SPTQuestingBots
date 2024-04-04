using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace SPTQuestingBots.Helpers
{
    public static class BotHelpers
    {
        private static FieldInfo pathControllerField = AccessTools.Field(typeof(BotMover), "_pathController");
        private static FieldInfo pathPointsField = AccessTools.Field(typeof(GClass466), "vector3_0");
        private static FieldInfo pathIndexField = AccessTools.Field(typeof(GClass466), "int_0");

        public static Vector3[] GetCurrentPath(this BotMover botMover)
        {
            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            Vector3[] path = (Vector3[])pathPointsField.GetValue(pathController.CurPath);

            return path;
        }

        public static int GetCurrentCornerIndex(this BotMover botMover)
        {
            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            int index = (int)pathIndexField.GetValue(pathController.CurPath);

            return index;
        }
    }
}
