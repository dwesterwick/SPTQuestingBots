using SPTarkov.Server.Core.DI;

namespace QuestingBots.Patches.Internal
{
    internal static class ServiceRepository
    {
        private static Dictionary<Type, object> serviceRepository = new Dictionary<Type, object>();

        public static T GetService<T>()
        {
            if (serviceRepository.ContainsKey(typeof(T)))
            {
                return (T)serviceRepository[typeof(T)];
            }

            T? service = ServiceLocator.ServiceProvider.GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Cannot resolve {typeof(T).Name} from ServiceProvider");
            }

            serviceRepository.Add(typeof(T), service);
            return service;
        }
    }
}
