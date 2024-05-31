import modConfig from "../config/config.json";
import { CommonUtils } from "./CommonUtils";

import type { DependencyContainer } from "tsyringe";
import type { IPreAkiLoadMod } from "@spt-aki/models/external/IPreAkiLoadMod";
import type { IPostDBLoadMod } from "@spt-aki/models/external/IPostDBLoadMod";
import type { IPostAkiLoadMod } from "@spt-aki/models/external/IPostAkiLoadMod";
import type { StaticRouterModService } from "@spt-aki/services/mod/staticRouter/StaticRouterModService";
import type { DynamicRouterModService } from "@spt-aki/services/mod/dynamicRouter/DynamicRouterModService";
import type { PreAkiModLoader } from "@spt-aki/loaders/PreAkiModLoader";

import type { MinMax } from "@spt-aki/models/common/MinMax";
import type { ConfigServer } from "@spt-aki/servers/ConfigServer";
import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import type { DatabaseServer } from "@spt-aki/servers/DatabaseServer";
import type { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import type { LocaleService } from "@spt-aki/services/LocaleService";
import type { QuestHelper } from "@spt-aki/helpers/QuestHelper";
import type { ProfileHelper } from "@spt-aki/helpers/ProfileHelper";
import type { VFS } from "@spt-aki/utils/VFS";
import type { HttpResponseUtil } from "@spt-aki/utils/HttpResponseUtil";
import type { RandomUtil } from "@spt-aki/utils/RandomUtil";
import type { BotController } from "@spt-aki/controllers/BotController";
import type { IGenerateBotsRequestData } from "@spt-aki/models/eft/bot/IGenerateBotsRequestData";
import type { IBotBase } from "@spt-aki/models/eft/common/tables/IBotBase";

import { ConfigTypes } from "@spt-aki/models/enums/ConfigTypes";
import type { IBotConfig } from "@spt-aki/models/spt/config/IBotConfig";
import type { IPmcConfig } from "@spt-aki/models/spt/config/IPmcConfig";
import type { ILocationConfig } from "@spt-aki/models/spt/config/ILocationConfig";
import type { IAirdropConfig } from "@spt-aki/models/spt/config/IAirdropConfig";

const modName = "SPTQuestingBots";

class QuestingBots implements IPreAkiLoadMod, IPostAkiLoadMod, IPostDBLoadMod
{
    private commonUtils: CommonUtils

    private logger: ILogger;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private localeService: LocaleService;
    private questHelper: QuestHelper;
    private profileHelper: ProfileHelper;
    private vfs: VFS;
    private httpResponseUtil: HttpResponseUtil;
    private randomUtil: RandomUtil;
    private botController: BotController;
    private iBotConfig: IBotConfig;
    private iPmcConfig: IPmcConfig;
    private iLocationConfig: ILocationConfig;
    private iAirdropConfig: IAirdropConfig;

    private convertIntoPmcChanceOrig: Record<string, MinMax> = {};
    private basePScavConversionChance: number;
	
    public preAkiLoad(container: DependencyContainer): void 
    {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
		
        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/QuestingBots/GetConfig",
                action: () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        ); 
        
        if (!modConfig.enabled)
        {
            return;
        }

        // Game start
        // Needed to update Scav timer
        staticRouterModService.registerStaticRouter(`StaticAkiGameStart${modName}`,
            [{
                url: "/client/game/start",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: (url: string, info: any, sessionId: string, output: string) => 
                {
                    if (modConfig.debug.enabled)
                    {
                        this.updateScavTimer(sessionId);
                    }

                    return output;
                }
            }], "aki"
        );

        // Apply a scalar factor to the SPT-AKI PMC conversion chances
        dynamicRouterModService.registerDynamicRouter(`DynamicAdjustPMCConversionChances${modName}`,
            [{
                url: "/QuestingBots/AdjustPMCConversionChances/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor: number = Number(urlParts[urlParts.length - 2]);
                    const verify: boolean = JSON.parse(urlParts[urlParts.length - 1].toLowerCase());

                    this.adjustPmcConversionChance(factor, verify);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "AdjustPMCConversionChances"
        );

        // Apply a scalar factor to the SPT-AKI PScav conversion chance
        dynamicRouterModService.registerDynamicRouter(`DynamicAdjustPScavChance${modName}`,
            [{
                url: "/QuestingBots/AdjustPScavChance/",
                action: (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor: number = Number(urlParts[urlParts.length - 1]);

                    this.iBotConfig.chanceAssaultScavHasPlayerScavName = Math.round(this.basePScavConversionChance * factor);
                    this.commonUtils.logInfo(`Adjusted PScav spawn chance to ${this.iBotConfig.chanceAssaultScavHasPlayerScavName}%`);

                    return JSON.stringify({ resp: "OK" });
                }
            }], "AdjustPScavChance"
        );
        
        // Get all EFT quest templates
        // NOTE: This includes custom quests added by mods
        staticRouterModService.registerStaticRouter(`GetAllQuestTemplates${modName}`,
            [{
                url: "/QuestingBots/GetAllQuestTemplates",
                action: () => 
                {
                    return JSON.stringify({ templates: this.questHelper.getQuestsFromDb() });
                }
            }], "GetAllQuestTemplates"
        );

        // Override bot generation to include PScav conversion chance
        dynamicRouterModService.registerDynamicRouter(`DynamicGenerateBot${modName}`,
            [{
                url: "/QuestingBots/GenerateBot",
                action: (url: string, info: IGenerateBotsRequestData, sessionID: string) => 
                {
                    const urlParts = url.split("/");
                    const pScavChance: number = Number(urlParts[urlParts.length - 1]);

                    const bots = this.generateBots(info, sessionID, this.randomUtil.getChance100(pScavChance));

                    return this.httpResponseUtil.getBody(bots);
                }
            }], "GenerateBot"
        );

        // Get Scav-raid settings to determine PScav conversion chances
        staticRouterModService.registerStaticRouter(`GetScavRaidSettings${modName}`,
            [{
                url: "/QuestingBots/GetScavRaidSettings",
                action: () => 
                {
                    return JSON.stringify({ maps: this.iLocationConfig.scavRaidTimeSettings.maps });
                }
            }], "GetScavRaidSettings"
        );
    }
	
    public postDBLoad(container: DependencyContainer): void
    {
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.localeService = container.resolve<LocaleService>("LocaleService");
        this.questHelper = container.resolve<QuestHelper>("QuestHelper");
        this.profileHelper = container.resolve<ProfileHelper>("ProfileHelper");
        this.vfs = container.resolve<VFS>("VFS");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.randomUtil = container.resolve<RandomUtil>("RandomUtil");
        this.botController = container.resolve<BotController>("BotController");

        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.iPmcConfig = this.configServer.getConfig(ConfigTypes.PMC);
        this.iLocationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);
        this.iAirdropConfig = this.configServer.getConfig(ConfigTypes.AIRDROP);

        this.databaseTables = this.databaseServer.getTables();
        this.basePScavConversionChance = this.iBotConfig.chanceAssaultScavHasPlayerScavName;
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);

        if (!modConfig.enabled)
        {
            return;
        }

        this.performFileIntegrityCheck();

        if (modConfig.debug.always_have_airdrops)
        {
            this.commonUtils.logInfo("Forcing airdrops to occur at the beginning of every raid...");

            this.iAirdropConfig.airdropChancePercent.bigmap = 100;
            this.iAirdropConfig.airdropChancePercent.woods = 100;
            this.iAirdropConfig.airdropChancePercent.lighthouse = 100;
            this.iAirdropConfig.airdropChancePercent.shoreline = 100;
            this.iAirdropConfig.airdropChancePercent.interchange = 100;
            this.iAirdropConfig.airdropChancePercent.reserve = 100;
            this.iAirdropConfig.airdropChancePercent.tarkovStreets = 100;
            this.iAirdropConfig.airdropChancePercent.sandbox = 100;

            this.iAirdropConfig.airdropMinStartTimeSeconds = 5;
            this.iAirdropConfig.airdropMaxStartTimeSeconds = 10;
        }

        // Adjust parameters to make debugging easier
        if (modConfig.debug.enabled)
        {
            this.commonUtils.logInfo("Applying debug options...");

            if (modConfig.debug.scav_cooldown_time < this.databaseTables.globals.config.SavagePlayCooldown)
            {
                this.databaseTables.globals.config.SavagePlayCooldown = modConfig.debug.scav_cooldown_time;
            }

            if (modConfig.debug.free_labs_access)
            {
                this.databaseTables.locations.laboratory.base.AccessKeys = [];
                this.databaseTables.locations.laboratory.base.DisabledForScav = false;
            }

            if (modConfig.debug.full_length_scav_raids)
            {
                this.forceFullLengthScavRaids();
            }
        }
    }
	
    public postAkiLoad(container: DependencyContainer): void
    {
        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json", true);
            return;
        }
        
        this.removeBlacklistedBrainTypes();

        // If we find SWAG, MOAR or BetterSpawnsPlus, disable initial spawns
        const preAkiModLoader = container.resolve<PreAkiModLoader>("PreAkiModLoader");
        if (modConfig.bot_spawns.enabled && preAkiModLoader.getImportedModsNames().includes("SWAG"))
        {
            this.commonUtils.logWarning("SWAG Detected. Disabling bot spawning.");
            modConfig.bot_spawns.enabled = false;
        }
        if (modConfig.bot_spawns.enabled && preAkiModLoader.getImportedModsNames().includes("DewardianDev-MOAR"))
        {
            this.commonUtils.logWarning("MOAR Detected. Disabling bot spawning.");
            modConfig.bot_spawns.enabled = false;
        }
        if (modConfig.bot_spawns.enabled && preAkiModLoader.getImportedModsNames().includes("PreyToLive-BetterSpawnsPlus")) 
        {
            this.commonUtils.logWarning("BetterSpawnsPlus Detected. Disabling bot spawning.");
            modConfig.bot_spawns.enabled = false;
        }

        if (preAkiModLoader.getImportedModsNames().includes("Andrudis-QuestManiac"))
        {
            this.commonUtils.logWarning("QuestManiac Detected. This mod is known to cause performance issues when used with QuestingBots. No support will be provided.");
        }

        // Make Questing Bots control PScav spawning
        if (modConfig.adjust_pscav_chance.enabled || (modConfig.bot_spawns.enabled && modConfig.bot_spawns.player_scavs.enabled))
        {
            this.iBotConfig.chanceAssaultScavHasPlayerScavName = 0;
        }

        if (!modConfig.bot_spawns.enabled)
        {
            return;
        }

        this.commonUtils.logInfo("Configuring game for bot spawning...");

        // Store the current PMC-conversion chances in case they need to be restored later
        this.setOriginalPMCConversionChances();

        // Currently these are all PMC waves, which are unnecessary with PMC spawns in this mod
        this.disableCustomBossWaves();

        // Disable all of the extra Scavs that spawn into Factory
        this.disableCustomScavWaves();

        // If Rogues don't spawn immediately, PMC spawns will be significantly delayed
        if (modConfig.bot_spawns.limit_initial_boss_spawns.disable_rogue_delay)
        {
            this.commonUtils.logInfo("Removing SPT Rogue spawn delay...");
            this.iLocationConfig.rogueLighthouseSpawnTimeSettings.waitTimeSeconds = -1;
        }

        if (modConfig.bot_spawns.advanced_eft_bot_count_management.enabled)
        {
            this.commonUtils.logInfo("Enabling advanced_eft_bot_count_management will instruct EFT to ignore this mod's PMC's and PScavs when spawning more bots.");
            this.useEFTBotCaps();
        }

        if (modConfig.bot_spawns.bot_cap_adjustments.enabled)
        {
            this.increaseBotCaps();
        }
        
        this.commonUtils.logInfo("Configuring game for bot spawning...done.");
    }

    private updateScavTimer(sessionId: string): void
    {
        const pmcData = this.profileHelper.getPmcProfile(sessionId);
        const scavData = this.profileHelper.getScavProfile(sessionId);
		
        if ((scavData.Info === null) || (scavData.Info === undefined))
        {
            this.commonUtils.logInfo("Scav profile hasn't been created yet.");
            return;
        }
		
        // In case somebody disables scav runs and later wants to enable them, we need to reset their Scav timer unless it's plausible
        const worstCooldownFactor = this.getWorstSavageCooldownModifier();
        if (scavData.Info.SavageLockTime - pmcData.Info.LastTimePlayedAsSavage > this.databaseTables.globals.config.SavagePlayCooldown * worstCooldownFactor * 1.1)
        {
            this.commonUtils.logInfo(`Resetting scav timer for sessionId=${sessionId}...`);
            scavData.Info.SavageLockTime = 0;
        }
    }
	
    // Return the highest Scav cooldown factor from Fence's rep levels
    private getWorstSavageCooldownModifier(): number
    {
        // Initialize the return value at something very low
        let worstCooldownFactor = 0.01;

        for (const level in this.databaseTables.globals.config.FenceSettings.Levels)
        {
            if (this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier > worstCooldownFactor)
                worstCooldownFactor = this.databaseTables.globals.config.FenceSettings.Levels[level].SavageCooldownModifier;
        }
        return worstCooldownFactor;
    }

    private setOriginalPMCConversionChances(): void
    {
        // Store the default PMC-conversion chances for each bot type defined in SPT's configuration file
        let logMessage = "";
        for (const pmcType in this.iPmcConfig.convertIntoPmcChance)
        {
            if (this.convertIntoPmcChanceOrig[pmcType] !== undefined)
            {
                logMessage += `${pmcType}: already buffered, `;
                continue;
            }

            const chances: MinMax = {
                min: this.iPmcConfig.convertIntoPmcChance[pmcType].min,
                max: this.iPmcConfig.convertIntoPmcChance[pmcType].max
            }
            this.convertIntoPmcChanceOrig[pmcType] = chances;

            logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
        }

        this.commonUtils.logInfo(`Reading default PMC spawn chances: ${logMessage}`);
    }

    private adjustPmcConversionChance(scalingFactor: number, verify: boolean): void
    {
        // Adjust the chances for each applicable bot type
        let logMessage = "";
        let verified = true;
        for (const pmcType in this.iPmcConfig.convertIntoPmcChance)
        {
            // Do not allow the chances to exceed 100%. Who knows what might happen...
            const min = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[pmcType].min * scalingFactor));
            const max = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[pmcType].max * scalingFactor));

            if (verify)
            {
                if (this.iPmcConfig.convertIntoPmcChance[pmcType].min !== min)
                {
                    verified = false;
                    break;
                }

                if (this.iPmcConfig.convertIntoPmcChance[pmcType].max !== max)
                {
                    verified = false;
                    break;
                }
            }
            else
            {
                this.iPmcConfig.convertIntoPmcChance[pmcType].min = min;
                this.iPmcConfig.convertIntoPmcChance[pmcType].max = max;

                logMessage += `${pmcType}: ${min}-${max}%, `;
            }
        }

        if (!verify)
        {
            this.commonUtils.logInfo(`Adjusting PMC spawn chances (${scalingFactor}): ${logMessage}`);
        }
        
        if (!verified)
        {
            this.commonUtils.logError("Another mod has changed the PMC conversion chances. This mod may not work properly!");
        }
    }

    private disableCustomBossWaves(): void
    {
        this.commonUtils.logInfo("Disabling custom boss waves...");
        this.iLocationConfig.customWaves.boss = {};
    }

    private disableCustomScavWaves(): void
    {
        this.commonUtils.logInfo("Disabling custom Scav waves...");
        this.iLocationConfig.customWaves.normal = {};
    }

    private increaseBotCaps(): void
    {
        if (!modConfig.bot_spawns.bot_cap_adjustments.add_max_players_to_bot_cap)
        {
            return;
        }

        const maxAddtlBots = modConfig.bot_spawns.bot_cap_adjustments.max_additional_bots;
        const maxTotalBots = modConfig.bot_spawns.bot_cap_adjustments.max_total_bots;

        this.iBotConfig.maxBotCap.factory4_day = Math.min(this.iBotConfig.maxBotCap.factory4_day + Math.min(this.databaseTables.locations.factory4_day.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.factory4_night = Math.min(this.iBotConfig.maxBotCap.factory4_night + Math.min(this.databaseTables.locations.factory4_night.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.bigmap = Math.min(this.iBotConfig.maxBotCap.bigmap + Math.min(this.databaseTables.locations.bigmap.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.woods = Math.min(this.iBotConfig.maxBotCap.woods + Math.min(this.databaseTables.locations.woods.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.shoreline = Math.min(this.iBotConfig.maxBotCap.shoreline + Math.min(this.databaseTables.locations.shoreline.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.lighthouse = Math.min(this.iBotConfig.maxBotCap.lighthouse + Math.min(this.databaseTables.locations.lighthouse.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.rezervbase = Math.min(this.iBotConfig.maxBotCap.rezervbase + Math.min(this.databaseTables.locations.rezervbase.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.interchange = Math.min(this.iBotConfig.maxBotCap.interchange + Math.min(this.databaseTables.locations.interchange.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.laboratory = Math.min(this.iBotConfig.maxBotCap.laboratory + Math.min(this.databaseTables.locations.laboratory.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.tarkovstreets = Math.min(this.iBotConfig.maxBotCap.tarkovstreets + Math.min(this.databaseTables.locations.tarkovstreets.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.sandbox = Math.min(this.iBotConfig.maxBotCap.sandbox + Math.min(this.databaseTables.locations.sandbox.base.MaxPlayers, maxAddtlBots), maxTotalBots);
        this.iBotConfig.maxBotCap.default = Math.min(this.iBotConfig.maxBotCap.default + maxAddtlBots, maxTotalBots);

        for (const location in this.iBotConfig.maxBotCap)
        {
            this.commonUtils.logInfo(`Changed bot cap for ${location} to: ${this.iBotConfig.maxBotCap[location]}`);
        }
    }

    private removeBlacklistedBrainTypes(): void
    {
        const badBrains = modConfig.bot_spawns.blacklisted_pmc_bot_brains;
        this.commonUtils.logInfo("Removing blacklisted brain types from being used for PMC's...");

        let removedBrains = 0;
        for (const pmcType in this.iPmcConfig.pmcType)
        {
            for (const map in this.iPmcConfig.pmcType[pmcType])
            {
                const mapBrains = this.iPmcConfig.pmcType[pmcType][map];
                
                for (const i in badBrains)
                {
                    if (mapBrains[badBrains[i]] === undefined)
                    {
                        continue;
                    }

                    //this.commonUtils.logInfo(`Removing ${badBrains[i]} from ${pmcType} in ${map}...`);
                    delete mapBrains[badBrains[i]];
                    removedBrains++;
                }
            }
        }

        this.commonUtils.logInfo(`Removing blacklisted brain types from being used for PMC's...done. Removed entries: ${removedBrains}`);
    }

    private forceFullLengthScavRaids(): void
    {
        this.commonUtils.logInfo("Forcing full-length Scav raids...");

        for (const map in this.iLocationConfig.scavRaidTimeSettings.maps)
        {
            this.iLocationConfig.scavRaidTimeSettings.maps[map].reducedChancePercent = 0;
        }
    }

    private generateBots(info: IGenerateBotsRequestData, sessionID: string, shouldBePScavGroup: boolean) : IBotBase[]
    {
        const bots = this.botController.generate(sessionID, info);

        if (!shouldBePScavGroup)
        {
            return bots;
        }

        const pmcNames = [
            ...this.databaseTables.bots.types.usec.firstName,
            ...this.databaseTables.bots.types.bear.firstName
        ];

        for (const bot in bots)
        {
            if (info.conditions[0].Role !== "assault")
            {
                continue;
            }

            bots[bot].Info.Nickname = `${bots[bot].Info.Nickname} (${this.randomUtil.getArrayValue(pmcNames)})`
        }

        return bots;
    }

    private performFileIntegrityCheck(): void
    {
        const path = `${__dirname}/..`;

        if (this.vfs.exists(`${path}/quests/`))
        {
            this.commonUtils.logWarning("Found obsolete quests folder 'user\\mods\\DanW-SPTQuestingBots\\quests'. Only quest files in 'BepInEx\\plugins\\DanW-SPTQuestingBots\\quests' will be used.");
        }

        if (this.vfs.exists(`${path}/log/`))
        {
            this.commonUtils.logWarning("Found obsolete log folder 'user\\mods\\DanW-SPTQuestingBots\\log'. Logs are now saved in 'BepInEx\\plugins\\DanW-SPTQuestingBots\\log'.");
        }
    }

    private useEFTBotCaps(): void
    {
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.use_EFT_bot_caps)
        {
            return;
        }

        this.commonUtils.logInfo(`Original bot counts for Factory Day - SPT: ${this.iBotConfig.maxBotCap.factory4_day}, EFT: ${this.databaseTables.locations.factory4_day.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Factory Night - SPT: ${this.iBotConfig.maxBotCap.factory4_night}, EFT: ${this.databaseTables.locations.factory4_night.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Customs - SPT: ${this.iBotConfig.maxBotCap.bigmap}, EFT: ${this.databaseTables.locations.bigmap.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Woods - SPT: ${this.iBotConfig.maxBotCap.woods}, EFT: ${this.databaseTables.locations.woods.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Shoreline - SPT: ${this.iBotConfig.maxBotCap.shoreline}, EFT: ${this.databaseTables.locations.shoreline.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Lighthouse - SPT: ${this.iBotConfig.maxBotCap.lighthouse}, EFT: ${this.databaseTables.locations.lighthouse.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Reserve - SPT: ${this.iBotConfig.maxBotCap.rezervbase}, EFT: ${this.databaseTables.locations.rezervbase.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Interchange - SPT: ${this.iBotConfig.maxBotCap.interchange}, EFT: ${this.databaseTables.locations.interchange.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Labs - SPT: ${this.iBotConfig.maxBotCap.laboratory}, EFT: ${this.databaseTables.locations.laboratory.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Streets - SPT: ${this.iBotConfig.maxBotCap.tarkovstreets}, EFT: ${this.databaseTables.locations.tarkovstreets.base.BotMax}`);
        this.commonUtils.logInfo(`Original bot counts for Ground Zero - SPT: ${this.iBotConfig.maxBotCap.sandbox}, EFT: ${this.databaseTables.locations.sandbox.base.BotMax}`);

        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.factory4_day > this.databaseTables.locations.factory4_day.base.BotMax))
        {
            this.iBotConfig.maxBotCap.factory4_day = this.databaseTables.locations.factory4_day.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.factory4_night > this.databaseTables.locations.factory4_night.base.BotMax))
        {
            this.iBotConfig.maxBotCap.factory4_night = this.databaseTables.locations.factory4_night.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.bigmap > this.databaseTables.locations.bigmap.base.BotMax))
        {
            this.iBotConfig.maxBotCap.bigmap = this.databaseTables.locations.bigmap.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.woods > this.databaseTables.locations.woods.base.BotMax))
        {
            this.iBotConfig.maxBotCap.woods = this.databaseTables.locations.woods.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.shoreline > this.databaseTables.locations.shoreline.base.BotMax))
        {
            this.iBotConfig.maxBotCap.shoreline = this.databaseTables.locations.shoreline.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.lighthouse > this.databaseTables.locations.lighthouse.base.BotMax))
        {
            this.iBotConfig.maxBotCap.lighthouse = this.databaseTables.locations.lighthouse.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.rezervbase > this.databaseTables.locations.rezervbase.base.BotMax))
        {
            this.iBotConfig.maxBotCap.rezervbase = this.databaseTables.locations.rezervbase.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.interchange > this.databaseTables.locations.interchange.base.BotMax))
        {
            this.iBotConfig.maxBotCap.interchange = this.databaseTables.locations.interchange.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.laboratory > this.databaseTables.locations.laboratory.base.BotMax))
        {
            this.iBotConfig.maxBotCap.laboratory = this.databaseTables.locations.laboratory.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.tarkovstreets > this.databaseTables.locations.tarkovstreets.base.BotMax))
        {
            this.iBotConfig.maxBotCap.tarkovstreets = this.databaseTables.locations.tarkovstreets.base.BotMax;
        }
        if (!modConfig.bot_spawns.advanced_eft_bot_count_management.only_decrease_bot_caps || (this.iBotConfig.maxBotCap.sandbox > this.databaseTables.locations.sandbox.base.BotMax))
        {
            this.iBotConfig.maxBotCap.sandbox = this.databaseTables.locations.sandbox.base.BotMax;
        }

        this.iBotConfig.maxBotCap.factory4_day += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.factory4_day;
        this.iBotConfig.maxBotCap.factory4_night += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.factory4_night;
        this.iBotConfig.maxBotCap.bigmap += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.bigmap;
        this.iBotConfig.maxBotCap.woods += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.woods;
        this.iBotConfig.maxBotCap.shoreline += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.shoreline;
        this.iBotConfig.maxBotCap.lighthouse += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.lighthouse;
        this.iBotConfig.maxBotCap.rezervbase += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.rezervbase;
        this.iBotConfig.maxBotCap.interchange += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.interchange;
        this.iBotConfig.maxBotCap.laboratory += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.laboratory;
        this.iBotConfig.maxBotCap.tarkovstreets += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.tarkovstreets;
        this.iBotConfig.maxBotCap.sandbox += modConfig.bot_spawns.advanced_eft_bot_count_management.bot_cap_adjustments.sandbox;

        this.commonUtils.logInfo(`Updated bot counts for Factory Day - SPT: ${this.iBotConfig.maxBotCap.factory4_day}, EFT: ${this.databaseTables.locations.factory4_day.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Factory Night - SPT: ${this.iBotConfig.maxBotCap.factory4_night}, EFT: ${this.databaseTables.locations.factory4_night.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Customs - SPT: ${this.iBotConfig.maxBotCap.bigmap}, EFT: ${this.databaseTables.locations.bigmap.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Woods - SPT: ${this.iBotConfig.maxBotCap.woods}, EFT: ${this.databaseTables.locations.woods.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Shoreline - SPT: ${this.iBotConfig.maxBotCap.shoreline}, EFT: ${this.databaseTables.locations.shoreline.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Lighthouse - SPT: ${this.iBotConfig.maxBotCap.lighthouse}, EFT: ${this.databaseTables.locations.lighthouse.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Reserve - SPT: ${this.iBotConfig.maxBotCap.rezervbase}, EFT: ${this.databaseTables.locations.rezervbase.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Interchange - SPT: ${this.iBotConfig.maxBotCap.interchange}, EFT: ${this.databaseTables.locations.interchange.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Labs - SPT: ${this.iBotConfig.maxBotCap.laboratory}, EFT: ${this.databaseTables.locations.laboratory.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Streets - SPT: ${this.iBotConfig.maxBotCap.tarkovstreets}, EFT: ${this.databaseTables.locations.tarkovstreets.base.BotMax}`);
        this.commonUtils.logInfo(`Updated bot counts for Ground Zero - SPT: ${this.iBotConfig.maxBotCap.sandbox}, EFT: ${this.databaseTables.locations.sandbox.base.BotMax}`);
    }
}

module.exports = { mod: new QuestingBots() }
