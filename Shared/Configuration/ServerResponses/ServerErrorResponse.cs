using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Configuration.ServerResponses
{
    public class ServerErrorResponse : ServerResponse
    {
        public ServerErrorResponse(string errorMessage) : base()
        {
            ErrorMessage = errorMessage;
            StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }
    }
}
