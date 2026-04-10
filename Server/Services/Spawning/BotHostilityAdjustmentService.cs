using QuestingBots.Helpers;
using QuestingBots.Services.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace QuestingBots.Services.Spawning
{
    [Injectable(TypePriority = OnLoadOrder.PostDBModLoader + QuestingBots_Server.LOAD_ORDER_OFFSET + 1)]
    public class BotHostilityAdjustmentService : AbstractService
    {
        private LoggingUtil _logger;
        private ConfigUtil _config;
        private DatabaseService _databaseService;
        private PmcConfig _pmcConfig;

        public BotHostilityAdjustmentService(LoggingUtil logger, ConfigUtil config, DatabaseService databaseService, ConfigServer configServer) : base(logger, config)
        {
            _logger = logger;
            _config = config;
            _databaseService = databaseService;
            _pmcConfig = configServer.GetConfig<PmcConfig>();
        }

        protected override void OnLoadIfModIsEnabled()
        {
            if (!_config.CurrentConfig.BotSpawns.Enabled)
            {
                return;
            }

            if (!_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.Enabled)
            {
                return;
            }

            AdjustAllBotHostilityChances();
        }

        private void AdjustAllBotHostilityChances()
        {
            _logger.Info("Adjusting bot hostility chances...");

            foreach (Location location in _databaseService.GetLocations().GetDictionary().Values)
            {
                AdjustAllBotHostilityChancesForLocation(location);
            }

            AdjustSptPmcHostilityChances(_pmcConfig.HostilitySettings["pmcbear"]);
            AdjustSptPmcHostilityChances(_pmcConfig.HostilitySettings["pmcusec"]);

            AdjustScavEnemyBotTypes();

            _logger.Info("Adjusting bot hostility chances...done.");
        }

        private void AdjustAllBotHostilityChancesForLocation(Location location)
        {
            if (location?.Base?.BotLocationModifier?.AdditionalHostilitySettings == null)
            {
                return;
            }

            foreach (AdditionalHostilitySettings settings in location.Base.BotLocationModifier.AdditionalHostilitySettings)
            {
                AdjustPMCBotHostilityChances(settings);
            }
        }

        private void AdjustPMCBotHostilityChances(AdditionalHostilitySettings settings)
        {
            if (!DatabaseHelpers.PMCRoleNames.Contains(settings.BotRole))
            {
                return;
            }

            if (settings.SavageEnemyChance != null)
            {
                settings.SavageEnemyChance = _config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.GlobalScavEnemyChance;
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstScavs)
            {
                settings.SavagePlayerBehaviour = "AlwaysEnemies";
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
            {
                settings.BearEnemyChance = 100;
                settings.UsecEnemyChance = 100;

                AddMissingPMCRolesToChancedEnemies(settings);
            }

            if (settings.ChancedEnemies == null)
            {
                return;
            }

            foreach (ChancedEnemy chancedEnemy in settings.ChancedEnemies)
            {
                if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCEnemyRoles.Contains(chancedEnemy.Role))
                {
                    chancedEnemy.EnemyChance = 100;
                    continue;
                }

                // This allows Questing Bots to set boss hostilities when the bot spawns
                chancedEnemy.EnemyChance = 0;
            }
        }

        private void AddMissingPMCRolesToChancedEnemies(AdditionalHostilitySettings settings)
        {
            foreach (string pmcRole in DatabaseHelpers.PMCRoleNames)
            {
                if (!_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCEnemyRoles.Contains(pmcRole))
                {
                    continue;
                }

                if (settings.ChancedEnemies == null)
                {
                    settings.ChancedEnemies = new List<ChancedEnemy>();
                }

                if (settings.ChancedEnemies.Any(enemy => enemy.Role == pmcRole) == true)
                {
                    continue;
                }

                ChancedEnemy enemy = new ChancedEnemy();
                enemy.Role = pmcRole;
                enemy.EnemyChance = 100;

                settings.ChancedEnemies.Add(enemy);
            }
        }

        private void AdjustSptPmcHostilityChances(HostilitySettings settings)
        {
            settings.SavageEnemyChance = _config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.GlobalScavEnemyChance;

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstScavs)
            {
                settings.SavagePlayerBehaviour = "AlwaysEnemies";
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
            {
                settings.BearEnemyChance = 100;
                settings.UsecEnemyChance = 100;
            }

            if (settings.ChancedEnemies == null)
            {
                return;
            }

            foreach (ChancedEnemy chancedEnemy in settings.ChancedEnemies)
            {
                if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCEnemyRoles.Contains(chancedEnemy.Role))
                {
                    chancedEnemy.EnemyChance = 100;
                    continue;
                }
            }
        }

        private void AdjustScavEnemyBotTypes()
        {
            if (!_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstScavs)
            {
                return;
            }

            foreach (string scavType in DatabaseHelpers.NormalScavRoleNames)
            {
                AdjustScavEnemyBotType(scavType);
            }
        }

        private void AdjustScavEnemyBotType(string role)
        {
            if (!_databaseService.GetBots().Types.ContainsKey(role))
            {
                return;
            }

            BotType? botType = _databaseService.GetBots().Types[role];
            if (botType == null)
            {
                return;
            }

            foreach (string difficulty in botType.BotDifficulty.Keys)
            {
                botType.BotDifficulty[difficulty].Mind.EnemyBotTypes = DatabaseHelpers.PMCRoles;
            }
        }
    }
}
