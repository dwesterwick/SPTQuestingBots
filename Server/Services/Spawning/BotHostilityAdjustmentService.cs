using QuestingBots.Helpers;
using QuestingBots.Services.Internal;
using QuestingBots.Utils;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace QuestingBots.Services.Spawning
{
    [Injectable(TypePriority = OnLoadOrder.Preload + QuestingBots_Server.LOAD_ORDER_OFFSET + 1)]
    public class BotHostilityAdjustmentService : AbstractService
    {
        private LoggingUtil _logger;
        private ConfigUtil _config;
        private LocationTable _locationTable;
        private BotTable _botTable;
        private PmcConfig _pmcConfig;

        public BotHostilityAdjustmentService(LoggingUtil logger, ConfigUtil config, LocationTable locationTable, BotTable botTable, PmcConfig pmcConfig) : base(logger, config)
        {
            _logger = logger;
            _config = config;
            _locationTable = locationTable;
            _botTable = botTable;
            _pmcConfig = pmcConfig;
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
            _logger.Info("Adjusting bot hostility chances...", true);

            foreach (Location location in _locationTable.GetDictionary().Values)
            {
                AdjustAllBotHostilityChancesForLocation(location);
            }

            AdjustSptPmcHostilityChances(_pmcConfig.HostilitySettings["pmcbear"]);
            AdjustSptPmcHostilityChances(_pmcConfig.HostilitySettings["pmcusec"]);

            AdjustScavEnemyBotTypes();

            _logger.Info("Adjusting bot hostility chances...done.", true);
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

                AddMissingRolesToChancedEnemies(settings.ChancedEnemies, DatabaseHelpers.NormalScavRoleNames);
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
            {
                settings.BearEnemyChance = 100;
                settings.UsecEnemyChance = 100;

                AddMissingRolesToChancedEnemies(settings.ChancedEnemies, DatabaseHelpers.PMCRoleNames);
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

        private void AddMissingRolesToChancedEnemies(List<ChancedEnemy>? chancedEnemies, IEnumerable<string> enemyRoles)
        {
            foreach (string enemyRole in enemyRoles)
            {
                if (!_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCEnemyRoles.Contains(enemyRole))
                {
                    continue;
                }

                if (chancedEnemies == null)
                {
                    chancedEnemies = new List<ChancedEnemy>();
                }

                if (chancedEnemies.Any(enemy => enemy.Role == enemyRole) == true)
                {
                    continue;
                }

                ChancedEnemy enemy = new ChancedEnemy();
                enemy.Role = enemyRole;
                enemy.EnemyChance = 100;

                chancedEnemies.Add(enemy);
            }
        }

        private void AdjustSptPmcHostilityChances(HostilitySettings settings)
        {
            settings.SavageEnemyChance = _config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.GlobalScavEnemyChance;

            if (settings.SavageEnemyChance != null)
            {
                settings.SavageEnemyChance = _config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.GlobalScavEnemyChance;
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstScavs)
            {
                settings.SavagePlayerBehaviour = "AlwaysEnemies";

                AddMissingRolesToChancedEnemies(settings.ChancedEnemies, DatabaseHelpers.NormalScavRoleNames);
            }

            if (_config.CurrentConfig.BotSpawns.PMCHostilityAdjustments.PMCsAlwaysHostileAgainstPMCs)
            {
                settings.BearEnemyChance = 100;
                settings.UsecEnemyChance = 100;

                AddMissingRolesToChancedEnemies(settings.ChancedEnemies, DatabaseHelpers.PMCRoleNames);
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
            if (!_botTable.Types.ContainsKey(role))
            {
                return;
            }

            BotType? botType = _botTable.Types[role];
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
