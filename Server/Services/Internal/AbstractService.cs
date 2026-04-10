using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPTarkov.Server.Core.DI;

namespace QuestingBots.Services.Internal;

public abstract class AbstractService : IOnLoad
{
    protected LoggingUtil Logger { get; private set; } = null!;
    protected ConfigUtil Config { get; private set; } = null!;

    private static bool _modDisabledMessageLogged = false;

    public AbstractService(LoggingUtil logger, ConfigUtil config)
    {
        Logger = logger;
        Config = config;
    }

    public Task OnLoad()
    {
        if (Config.CurrentConfig.IsModEnabled())
        {
            OnLoadIfModIsEnabled();
        }
        else
        {
            LogModDisabledMessage();
        }

        return Task.CompletedTask;
    }

    protected abstract void OnLoadIfModIsEnabled();

    private void LogModDisabledMessage()
    {
        if (_modDisabledMessageLogged)
        {
            return;
        }

        Logger.Info(ModInfo.MODNAME + " is disabled.");

        _modDisabledMessageLogged = true;
    }
}
