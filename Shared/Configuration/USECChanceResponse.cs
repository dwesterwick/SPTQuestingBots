using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class USECChanceResponse
    {
        [DataMember(Name = "usecChance")]
        public float USECChance { get; set; } = 50;

        public USECChanceResponse()
        {

        }
    }
}
