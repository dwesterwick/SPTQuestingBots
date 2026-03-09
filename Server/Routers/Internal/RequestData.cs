using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Routers.Internal
{
    public struct RequestData
    {
        public RequestData(string url, IRequestData info, MongoId sessionId, string? output)
        {
            Url = url;
            Info = info;
            SessionId = sessionId;
            Output = output;
        }

        public string Url { get; init; }
        public IRequestData Info { get; init; }
        public MongoId SessionId { get; init; }
        public string? Output { get; init; }
    }
}
