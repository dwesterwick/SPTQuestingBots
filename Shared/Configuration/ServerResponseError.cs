using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ServerResponseError
    {
        [DataMember(Name = "err")]
        public System.Net.HttpStatusCode StatusCode { get; set; } = System.Net.HttpStatusCode.OK;

        [DataMember(Name = "errmsg")]
        public string ErrorMessage { get; set; } = "";

        [DataMember(Name = "data")]
        public object Data { get; set; } = null!;

        public ServerResponseError()
        {

        }
    }
}
