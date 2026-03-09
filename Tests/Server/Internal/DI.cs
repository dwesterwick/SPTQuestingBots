using Microsoft.Extensions.DependencyInjection;
using SPTarkov.DI;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Logger.Handlers;

namespace QuestingBots.Server.Internal;

// Copied from https://github.com/sp-tarkov/server-csharp/blob/main/Testing/UnitTests/DI.cs

[TestFixture]
public class DI
{
    private static IServiceProvider _serviceProvider = null!;

    private static DI? _instance;

    private DI()
    {
        ConfigureServices();
    }

    public static DI GetInstance()
    {
        return _instance ??= new DI();
    }

    private void ConfigureServices()
    {
        if (_serviceProvider != null)
        {
            return;
        }

        var services = new ServiceCollection();

        var diHandler = new DependencyInjectionHandler(services);

        diHandler.AddInjectableTypesFromTypeAssembly(typeof(App));
        diHandler.AddInjectableTypesFromTypeList([
            typeof(MockLogger<>), // TODO: this needs to be enabled but the randomizer needs to NOT be random, typeof(MockRandomUtil)
        ]);

        diHandler.InjectAll();

        services.AddSingleton<IReadOnlyList<SptMod>>(_ => []);

        _serviceProvider = services.BuildServiceProvider();

        foreach (var onLoad in _serviceProvider.GetServices<IOnLoad>())
        {
            if (onLoad is FileLogHandler)
            {
                continue;
            }
            onLoad.OnLoad().Wait();
        }
    }

    public T GetService<T>()
        where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }
}