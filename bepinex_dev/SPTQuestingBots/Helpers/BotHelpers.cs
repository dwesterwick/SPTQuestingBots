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
        private static FieldInfo pathField = AccessTools.Field(typeof(GClass466), "vector3_0");

        public static Vector3[] CurPath(this BotMover botMover)
        {
            PathControllerClass pathController = (PathControllerClass)pathControllerField.GetValue(botMover);
            Vector3[] path = (Vector3[])pathControllerField.GetValue(pathController.CurPath);

            return path;
        }
    }
}
