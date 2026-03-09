using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Utils;

namespace QuestingBots.Routers.Internal
{
    [Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 1)]
    public class HTTPResponseRepository
    {
        public static string NullResponseText { get; private set; } = null!;

        private HttpResponseUtil _httpResponseUtil;

        public static ValueTask<string?> NullResponse => new ValueTask<string?>(NullResponseText);

        public HTTPResponseRepository(HttpResponseUtil httpResponseUtil)
        {
            _httpResponseUtil = httpResponseUtil;

            CreateReponses();
        }

        private void CreateReponses()
        {
            NullResponseText = _httpResponseUtil.NullResponse();
        }
    }
}
