using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    public class MinMaxConfig
    {
        [JsonProperty("min")]
        public double Min { get; set; } = 0;

        [JsonProperty("max")]
        public double Max { get; set; } = 100;

        public MinMaxConfig()
        {

        }
    }
}
