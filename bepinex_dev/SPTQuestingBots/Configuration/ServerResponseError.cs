using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class ServerResponseError
    {
        //[JsonConverter(typeof(EnumConverter))]
        [JsonProperty("err")]
        public System.Net.HttpStatusCode StatusCode { get; set; } = System.Net.HttpStatusCode.OK;

        [JsonProperty("errmsg")]
        public string ErrorMessage { get; set; } = "";

        [JsonProperty("data")]
        public object Data { get; set; } = null;

        public ServerResponseError()
        {

        }
    }
}
