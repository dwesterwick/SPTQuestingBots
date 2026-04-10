using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Helpers
{
    public static class UnityHelpers
    {
        public static Vector3 ToUnityVector3(this SerializableVector3 vector)
        {
            return new Vector3(vector.X, vector.Y, vector.Z);
        }

        public static SerializableVector3 ToSerializableVector3(this Vector3 vector)
        {
            return new SerializableVector3(vector.x, vector.y, vector.z);
        }
    }
}
