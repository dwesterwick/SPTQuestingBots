using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class DistanceAngleConfig
    {
        [DataMember(Name = "distance", IsRequired = true)]
        public float Distance { get; set; } = 0;

        [DataMember(Name = "angle", IsRequired = true)]
        public float Angle { get; set; } = 90;

        public DistanceAngleConfig()
        {

        }
    }
}
