import modConfig from "../config/config.json";

import type { CommonUtils } from "./CommonUtils";
import type { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";

import type { ILocationConfig } from "@spt/models/spt/config/ILocationConfig";
import type { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import type { ILocation } from "@spt/models/eft/common/ILocation";
import type { IAdditionalHostilitySettings, IBossLocationSpawn, IChancedEnemy } from "@spt/models/eft/common/ILocationBase";

export class BotUtil
{
    private static readonly pmcRoles = ["pmcBEAR", "pmcUSEC"];

    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables, private iLocationConfig: ILocationConfig, private iBotConfig: IBotConfig)
    {

    }

    public adjustAllBotHostilityChances(): void
    {
        if (!modConfig.bot_spawns.pmc_hostility_adjustments.enabled)
        {
            return;
        }

        this.commonUtils.logInfo("Adjusting bot hostility chances...");

        for (const location in this.databaseTables.locations)
        {
            this.adjustAllBotHostilityChancesForLocation(this.databaseTables.locations[location]);
        }

        if (modConfig.bot_spawns.pmc_hostility_adjustments.pmcs_always_hostile_against_scavs)
        {
            this.databaseTables.bots.types.assault.difficulty.easy.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assault.difficulty.normal.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assault.difficulty.hard.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assault.difficulty.impossible.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;

            this.databaseTables.bots.types.assaultgroup.difficulty.easy.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assaultgroup.difficulty.normal.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assaultgroup.difficulty.hard.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.assaultgroup.difficulty.impossible.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;

            this.databaseTables.bots.types.marksman.difficulty.easy.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.marksman.difficulty.normal.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.marksman.difficulty.hard.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
            this.databaseTables.bots.types.marksman.difficulty.impossible.Mind.ENEMY_BOT_TYPES = BotUtil.pmcRoles;
        }
    }

    private adjustAllBotHostilityChancesForLocation(location : ILocation): void
    {
        if ((location.base === undefined) || (location.base.BotLocationModifier === undefined))
        {
            return;
        }
        
        const settings = location.base.BotLocationModifier.AdditionalHostilitySettings;
        if (settings === undefined)
        {
            return;
        }

        for (const botType in settings)
        {
            if (!BotUtil.pmcRoles.includes(settings[botType].BotRole))
            {
                this.commonUtils.logWarning(`Did not adjust ${settings[botType].BotRole} hostility settings on ${location.base.Name}`);
                continue;
            }

            this.adjustBotHostilityChances(settings[botType]);
        }
    }

    private adjustBotHostilityChances(settings: IAdditionalHostilitySettings): void
    {
        // This seems to be undefined for most maps
        if (settings.SavageEnemyChance !== undefined)
        {
            settings.SavageEnemyChance = modConfig.bot_spawns.pmc_hostility_adjustments.global_scav_enemy_chance;
        }

        if (modConfig.bot_spawns.pmc_hostility_adjustments.pmcs_always_hostile_against_scavs)
        {
            settings.SavagePlayerBehaviour = "AlwaysEnemies";
        }

        for (const chancedEnemy in settings.ChancedEnemies)
        {
            if (modConfig.bot_spawns.pmc_hostility_adjustments.pmc_enemy_roles.includes(settings.ChancedEnemies[chancedEnemy].Role))
            {
                settings.ChancedEnemies[chancedEnemy].EnemyChance = 100;
                continue;
            }
            
            // This allows Questing Bots to set boss hostilities when the bot spawns
            settings.ChancedEnemies[chancedEnemy].EnemyChance = 0;
        }

        if (modConfig.bot_spawns.pmc_hostility_adjustments.pmcs_always_hostile_against_pmcs)
        {
            settings.BearEnemyChance = 100;
            settings.UsecEnemyChance = 100;

            this.addMissingPMCRolesToChancedEnemies(settings);
        }
    }

    private addMissingPMCRolesToChancedEnemies(settings: IAdditionalHostilitySettings): void
    {
        for (const pmcRole of BotUtil.pmcRoles)
        {
            if (!modConfig.bot_spawns.pmc_hostility_adjustments.pmc_enemy_roles.includes(pmcRole))
            {
                continue;
            }
            
            let foundRole = false;
            for (const chancedEnemy in settings.ChancedEnemies)
            {
                if (settings.ChancedEnemies[chancedEnemy].Role === pmcRole)
                {
                    foundRole = true;
                    break;
                }
            }

            if (foundRole)
            {
                continue;
            }
            
            const newEnemy : IChancedEnemy = 
            {
                EnemyChance: 100,
                Role: pmcRole
            };
            
            settings.ChancedEnemies.push(newEnemy);
        }
    }

    public disablePvEBossWaves(): void
    {
        this.commonUtils.logInfo("Disabling PvE boss waves...");

        let removedWaves = 0;
        for (const location in this.databaseTables.locations)
        {
            removedWaves += this.removePvEBossWavesFromLocation(this.databaseTables.locations[location]);
        }

        this.commonUtils.logInfo(`Disabled ${removedWaves} PvE boss waves`);
    }

    private removePvEBossWavesFromLocation(location : ILocation): number
    {
        let removedWaves = 0;

        if ((location.base === undefined) || (location.base.BossLocationSpawn === undefined))
        {
            return removedWaves;
        }

        const modifiedBossLocationSpawn : IBossLocationSpawn[] = [];

        for (const bossLocationSpawnId in location.base.BossLocationSpawn)
        {
            const bossLocationSpawn = location.base.BossLocationSpawn[bossLocationSpawnId];

            if (BotUtil.pmcRoles.includes(bossLocationSpawn.BossName))
            {
                removedWaves++;
                continue;
            }

            modifiedBossLocationSpawn.push(bossLocationSpawn);
        }

        location.base.BossLocationSpawn = modifiedBossLocationSpawn;

        return removedWaves;
    }

    public disableCustomBossWaves(): void
    {
        this.commonUtils.logInfo("Disabling custom boss waves...");
        this.iLocationConfig.customWaves.boss = {};
    }

    public disableCustomScavWaves(): void
    {
        this.commonUtils.logInfo("Disabling custom Scav waves...");
        this.iLocationConfig.customWaves.normal = {};
    }

    public increaseBotCaps(): void
    {
        if (!modConfig.bot_spawns.bot_cap_adjustments.add_max_players_to_bot_cap)
        {
            return;
        }

        const maxAddtlBots = modConfig.bot_spawns.bot_cap_adjustments.max_additional_bots;
        const maxTotalBots = modConfig.bot_spawns.bot_cap_adjustments.max_total_bots;

        for (const location in this.iBotConfig.maxBotCap)
        {
            if (this.databaseTables.locations[location].base === undefined)
            {
                continue;
            }
            
            const maxPlayers = this.databaseTables.locations[location].base.MaxPlayers;
            this.iBotConfig.maxBotCap[location] = Math.min(this.iBotConfig.maxBotCap[location] + Math.min(maxPlayers, maxAddtlBots), maxTotalBots);

            this.commonUtils.logInfo(`Changed bot cap for ${location} to: ${this.iBotConfig.maxBotCap[location]}`);
        }

        this.iBotConfig.maxBotCap.default = Math.min(this.iBotConfig.maxBotCap.default + maxAddtlBots, maxTotalBots);
        this.commonUtils.logInfo(`Changed default bot cap to: ${this.iBotConfig.maxBotCap.default}`);
    }

    public useEFTBotCaps(): void
    {
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.use_EFT_bot_caps)
        {
            return;
        }

        for (const location in this.iBotConfig.maxBotCap)
        {
            if ((this.databaseTables.locations[location] === undefined) || (this.databaseTables.locations[location].base === undefined))
            {
                continue;
            }

            const originalSPTCap = this.iBotConfig.maxBotCap[location];
            const eftCap = this.databaseTables.locations[location].base.BotMax;

            if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (originalSPTCap > eftCap))
            {
                this.iBotConfig.maxBotCap[location] = eftCap;
            }

            const fixedAdjustment = modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments[location];
            this.iBotConfig.maxBotCap[location] += fixedAdjustment;

            const newCap = this.iBotConfig.maxBotCap[location];
            this.commonUtils.logInfo(`Updated bot cap for ${location} to ${newCap} (Original SPT: ${originalSPTCap}, EFT: ${eftCap}, fixed adjustment: ${fixedAdjustment})`);
        }
    }

    public modifyNonWaveBotSpawnSettings(): void
    {
        this.commonUtils.logInfo("Updating BotSpawnPeriodCheck for all maps...");

        for (const location in this.iBotConfig.maxBotCap)
        {
            if ((this.databaseTables.locations[location] === undefined) || (this.databaseTables.locations[location].base === undefined))
            {
                continue;
            }
            
            this.databaseTables.locations[location].base.BotSpawnPeriodCheck *= modConfig.bot_spawns.non_wave_bot_spawn_period_factor;
        }
    }
}