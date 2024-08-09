using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class USECChanceResponse
    {
        [JsonProperty("usecChance")]
        public float USECChance { get; set; } = 50;

        public USECChanceResponse()
        {

        }
    }
}
