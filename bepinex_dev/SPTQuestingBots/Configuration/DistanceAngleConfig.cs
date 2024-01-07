using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class DistanceAngleConfig
    {
        [JsonProperty("distance")]
        public float Distance { get; set; } = 0;

        [JsonProperty("angle")]
        public float Angle { get; set; } = 90;

        public DistanceAngleConfig()
        {

        }
    }
}
