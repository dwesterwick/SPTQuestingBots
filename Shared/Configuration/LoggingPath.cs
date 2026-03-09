using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LoggingPath
    {
        [DataMember(Name = "path")]
        public string Path { get; set; } = "";

        public LoggingPath()
        {

        }
    }
}
