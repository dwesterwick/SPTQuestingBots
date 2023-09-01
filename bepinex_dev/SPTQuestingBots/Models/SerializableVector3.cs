using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class SerializableVector3
    {
        [JsonProperty("x")]
        public float X { get; set; } = float.NaN;

        [JsonProperty("y")]
        public float Y { get; set; } = float.NaN;

        [JsonProperty("z")]
        public float Z { get; set; } = float.NaN;

        public SerializableVector3()
        {

        }

        public SerializableVector3(float x, float y, float z) : this()
        {
            X = x;
            Y = y;
            Z = z;
        }

        public SerializableVector3(Vector3 vector) : this()
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToUnityVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public bool Any(float n)
        {
            if (X.Equals(n)) return true;
            if (Y.Equals(n)) return true;
            if (Z.Equals(n)) return true;

            return false;
        }
    }
}
