using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Models
{
    [DataContract]
    public class SerializableVector3
    {
        [DataMember(Name = "x")]
        public float X { get; set; } = float.NaN;

        [DataMember(Name = "y")]
        public float Y { get; set; } = float.NaN;

        [DataMember(Name = "z")]
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

        public bool Any(float n)
        {
            if (X.Equals(n)) return true;
            if (Y.Equals(n)) return true;
            if (Z.Equals(n)) return true;

            return false;
        }
    }
}
