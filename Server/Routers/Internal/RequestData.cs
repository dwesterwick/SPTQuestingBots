using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Routers.Internal
{
    public struct RequestData
    {
        public RequestData(string url, IRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken)
        {
            Url = url;
            Info = info;
            SessionId = sessionId;
            Output = output;
            CancellationToken = cancellationToken;
        }

        public string Url { get; init; }
        public IRequestData Info { get; init; }
        public MongoId SessionId { get; init; }
        public CancellationToken CancellationToken { get; init; }
        public string? Output { get; init; }
    }
}
