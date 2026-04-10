using QuestingBots.Helpers;
using QuestingBots.Services.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace QuestingBots.Services
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET)]
    public class AutoDisableSpawningSystemService : AbstractService
    {
        private readonly string[] SPAWNING_MOD_GUIDS = ["li.barlog.unda", "com.acidphantasm.botplacementsystem"];

        private LoggingUtil _logger;
        private ConfigUtil _config;
        private IReadOnlyList<SptMod> _loadedMods;

        public AutoDisableSpawningSystemService(LoggingUtil logger, ConfigUtil config, IReadOnlyList<SptMod> loadedMods) : base(logger, config)
        {
            _logger = logger;
            _config = config;
            _loadedMods = loadedMods;
        }

        protected override void OnLoadIfModIsEnabled()
        {
            if (_config.CurrentConfig.IsDebugEnabled())
            {
                _logger.Info($"Loaded mod GUIDs: {string.Join(", ", _loadedMods.Select(mod => mod.ModMetadata.ModGuid))}");
            }

            DisableSpawningSystemIfAnotherSpawningModIsLoaded();
        }

        private void DisableSpawningSystemIfAnotherSpawningModIsLoaded()
        {
            if (!_config.CurrentConfig.BotSpawns.Enabled)
            {
                return;
            }

            foreach (SptMod mod in _loadedMods)
            {
                if (SPAWNING_MOD_GUIDS.Contains(mod.ModMetadata.ModGuid))
                {
                    _logger.Warning($"{mod.ModMetadata.Name} detected. Disabling the Questing Bots spawning system.");

                    _config.CurrentConfig.BotSpawns.Enabled = false;
                    return;
                }
            }
        }
    }
}
