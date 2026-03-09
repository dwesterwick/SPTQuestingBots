using QuestingBots.Helpers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace QuestingBots.Routers.Internal
{
    internal class TypedRouteInfo<T> : IRouteInfo where T: class, IRequestData
    {
        public string Name { get; private set; }
        public IRouteHandler Handler { get; private set; }

        public TypedRouteInfo(string routeName, IRouteHandler routerInstance)
        {
            Name = routeName;
            Handler = routerInstance;
        }

        private string _path = null!;
        public string Path
        {
            get
            {
                if (_path == null)
                {
                    _path = SharedRouterHelpers.GetRoutePath(Name);
                }
                return _path;
            }
        }

        private RouteAction? _action = null;
        public RouteAction? Action
        {
            get
            {
                if (_action == null)
                {
                    _action = CreateRouteAction();
                }

                return _action;
            }
        }

        private RouteAction? CreateRouteAction()
        {
            if (!Handler.ShouldCreateRoutes())
            {
                return null;
            }

            Func<string, IRequestData, MongoId, string?, ValueTask<object>> func = async (url, info, sessionId, output) =>
                        await HandleRoute(Name, url, info, sessionId, output) ?? throw new InvalidOperationException("HandleRoute returned null");

            Func<string, IRequestData, MongoId, string?, ValueTask<string?>> funcTyped = async (url, info, sessionId, output) =>
                        await HandleRoute(Name, url, info, sessionId, output) ?? throw new InvalidOperationException("HandleRoute returned null");

            bool useTypedAction = typeof(T) != typeof(EmptyRequestData);
            if (useTypedAction)
            {
                return new RouteAction<T>(Path, funcTyped!);
            }

            return new RouteAction(Path, func!);
        }

        private async ValueTask<string?> HandleRoute(string routeName, string url, IRequestData info, MongoId sessionId, string? output)
        {
            RequestData requestData = new RequestData(url, info, sessionId, output);
            return await HandleRoute(routeName, requestData);
        }

        private ValueTask<string?> HandleRoute(string name, RequestData data)
        {
            if (!Handler.ShouldHandleRoutes())
            {
                return HTTPResponseRepository.NullResponse;
            }

            return Handler.HandleRoute(name, data);
        }
    }
}
