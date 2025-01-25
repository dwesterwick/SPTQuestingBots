import modConfig from "../config/config.json";
import eftQuestSettings from "../config/eftQuestSettings.json";
import eftZoneAndItemPositions from "../config/zoneAndItemQuestPositions.json";
import { CommonUtils } from "./CommonUtils";
import { BotUtil } from "./BotLocationUtil";
import { PMCConversionUtil } from "./PMCConversionUtil";

import type { DependencyContainer } from "tsyringe";
import type { IPreSptLoadMod } from "@spt/models/external/IPreSptLoadMod";
import type { IPostDBLoadMod } from "@spt/models/external/IPostDBLoadMod";
import type { IPostSptLoadMod } from "@spt/models/external/IPostSptLoadMod";
import type { StaticRouterModService } from "@spt/services/mod/staticRouter/StaticRouterModService";
import type { DynamicRouterModService } from "@spt/services/mod/dynamicRouter/DynamicRouterModService";
import type { PreSptModLoader } from "@spt/loaders/PreSptModLoader";

import type { ConfigServer } from "@spt/servers/ConfigServer";
import type { ILogger } from "@spt/models/spt/utils/ILogger";
import type { DatabaseServer } from "@spt/servers/DatabaseServer";
import type { IDatabaseTables } from "@spt/models/spt/server/IDatabaseTables";
import type { LocaleService } from "@spt/services/LocaleService";
import type { QuestHelper } from "@spt/helpers/QuestHelper";
import type { VFS } from "@spt/utils/VFS";
import type { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";
import type { RandomUtil } from "@spt/utils/RandomUtil";
import type { BotController } from "@spt/controllers/BotController";
import type { BotCallbacks } from "@spt/callbacks/BotCallbacks";
import type { IGenerateBotsRequestData, ICondition } from "@spt/models/eft/bot/IGenerateBotsRequestData";
import type { IBotBase } from "@spt/models/eft/common/tables/IBotBase";

import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import type { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import type { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";
import type { ILocationConfig } from "@spt/models/spt/config/ILocationConfig";

const modName = "SPTQuestingBots";
const spawningModNames = ["SWAG", "DewardianDev-MOAR", "PreyToLive-BetterSpawnsPlus", "RealPlayerSpawn"];

class QuestingBots implements IPreSptLoadMod, IPostSptLoadMod, IPostDBLoadMod
{
    private commonUtils: CommonUtils
    private botUtil: BotUtil
    private pmcConversionUtil : PMCConversionUtil

    private logger: ILogger;
    private configServer: ConfigServer;
    private databaseServer: DatabaseServer;
    private databaseTables: IDatabaseTables;
    private localeService: LocaleService;
    private questHelper: QuestHelper;
    private vfs: VFS;
    private httpResponseUtil: HttpResponseUtil;
    private randomUtil: RandomUtil;
    private botController: BotController;
    private iBotConfig: IBotConfig;
    private iPmcConfig: IPmcConfig;
    private iLocationConfig: ILocationConfig;

    private basePScavConversionChance: number;

    public preSptLoad(container: DependencyContainer): void 
    {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
		
        // Cache and adjust the PMC conversion chances
        staticRouterModService.registerStaticRouter(`StaticRaidConfiguration${modName}`,
            [{
                url: "/client/game/start",
                // biome-ignore lint/suspicious/noExplicitAny: <explanation>
                action: async (url: string, info: any, sessionId: string, output: string) => 
                {
                    if (modConfig.bot_spawns.enabled)
                    {
                        this.pmcConversionUtil.adjustAllPmcConversionChances(0, false);
                    }

                    return output;
                }
            }], "aki"
        );

        // Get config.json settings for the bepinex plugin
        staticRouterModService.registerStaticRouter(`StaticGetConfig${modName}`,
            [{
                url: "/QuestingBots/GetConfig",
                action: async () => 
                {
                    return JSON.stringify(modConfig);
                }
            }], "GetConfig"
        ); 
        
        if (!modConfig.enabled)
        {
            return;
        }

        // Apply a scalar factor to the SPT-AKI PMC conversion chances
        dynamicRouterModService.registerDynamicRouter(`DynamicAdjustPMCConversionChances${modName}`,
            [{
                url: "/QuestingBots/AdjustPMCConversionChances/",
                action: async (url: string) => 
                {
                    const urlParts = url.split("/");
                    const factor: number = Number(urlParts[urlParts.length - 2]);
                    const verify: boolean = JSON.parse(urlParts[urlParts.length - 1].toLowerCase());

                    this.pmcConversionUtil.adjustAllPmcConversionChances(factor, verify);
                    return JSON.stringify({ resp: "OK" });
                }
            }], "AdjustPMCConversionChances"
        );

        // Apply a scalar factor to the SPT-AKI PScav conversion chance
        dynamicRouterModService.registerDynamicRouter(`DynamicAdjustPScavChance${modName}`,
            [{
                url: "/QuestingBots/AdjustPScavChance/",
                action: async (url: string) => 
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
                action: async () => 
                {
                    return JSON.stringify({ templates: this.questHelper.getQuestsFromDb() });
                }
            }], "GetAllQuestTemplates"
        );

        // Get override settings for EFT quests
        staticRouterModService.registerStaticRouter(`GetEFTQuestSettings${modName}`,
            [{
                url: "/QuestingBots/GetEFTQuestSettings",
                action: async () => 
                {
                    return JSON.stringify({ settings: eftQuestSettings });
                }
            }], "GetEFTQuestSettings"
        );

        // Get override settings for quest zones and items
        staticRouterModService.registerStaticRouter(`GetZoneAndItemQuestPositions${modName}`,
            [{
                url: "/QuestingBots/GetZoneAndItemQuestPositions",
                action: async () => 
                {
                    return JSON.stringify({ zoneAndItemPositions: eftZoneAndItemPositions });
                }
            }], "GetZoneAndItemQuestPositions"
        );

        // Get Scav-raid settings to determine PScav conversion chances
        staticRouterModService.registerStaticRouter(`GetScavRaidSettings${modName}`,
            [{
                url: "/QuestingBots/GetScavRaidSettings",
                action: async () => 
                {
                    return JSON.stringify({ maps: this.iLocationConfig.scavRaidTimeSettings.maps });
                }
            }], "GetScavRaidSettings"
        );

        // Get the chance that a PMC will be a USEC
        staticRouterModService.registerStaticRouter(`GetUSECChance${modName}`,
            [{
                url: "/QuestingBots/GetUSECChance",
                action: async () => 
                {
                    return JSON.stringify({ usecChance: this.iPmcConfig.isUsec });
                }
            }], "GetUSECChance"
        );

        // Intercept the EFT bot-generation request to include a PScav conversion chance
        container.afterResolution("BotCallbacks", (_t, result: BotCallbacks) =>
        {
            result.generateBots = async (url: string, info: IGenerateBotsRequestDataWithPScavChance, sessionID: string) =>
            {
                const bots = await this.generateBots({ conditions: info.conditions }, sessionID, this.randomUtil.getChance100(info.PScavChance));
                return this.httpResponseUtil.getBody(bots);
            }
        }, {frequency: "Always"});
    }
	
    public postDBLoad(container: DependencyContainer): void
    {
        this.configServer = container.resolve<ConfigServer>("ConfigServer");
        this.databaseServer = container.resolve<DatabaseServer>("DatabaseServer");
        this.localeService = container.resolve<LocaleService>("LocaleService");
        this.questHelper = container.resolve<QuestHelper>("QuestHelper");
        this.vfs = container.resolve<VFS>("VFS");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.randomUtil = container.resolve<RandomUtil>("RandomUtil");
        this.botController = container.resolve<BotController>("BotController");

        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.iPmcConfig = this.configServer.getConfig(ConfigTypes.PMC);
        this.iLocationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);

        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);
        this.botUtil = new BotUtil(this.commonUtils, this.databaseTables, this.iLocationConfig, this.iBotConfig);
        this.pmcConversionUtil = new PMCConversionUtil(this.commonUtils, this.iPmcConfig);

        if (!modConfig.enabled)
        {
            return;
        }

        if (!this.doesFileIntegrityCheckPass())
        {
            modConfig.enabled = false;
            return;
        }
    }
	
    public postSptLoad(container: DependencyContainer): void
    {
        if (!modConfig.enabled)
        {
            this.commonUtils.logInfo("Mod disabled in config.json", true);
            return;
        }

        const presptModLoader = container.resolve<PreSptModLoader>("PreSptModLoader");
        
        this.pmcConversionUtil.removeBlacklistedBrainTypes();

        // Disable the Questing Bots spawning system if another spawning mod has been loaded
        if (this.shouldDisableSpawningSystem(presptModLoader.getImportedModsNames()))
        {
            modConfig.bot_spawns.enabled = false;
        }

        // Make Questing Bots control PScav spawning
        this.basePScavConversionChance = this.iBotConfig.chanceAssaultScavHasPlayerScavName;
        if (modConfig.adjust_pscav_chance.enabled || (modConfig.bot_spawns.enabled && modConfig.bot_spawns.player_scavs.enabled))
        {
            this.iBotConfig.chanceAssaultScavHasPlayerScavName = 0;
        }

        this.configureSpawningSystem();
    }

    private configureSpawningSystem(): void
    {
        if (!modConfig.bot_spawns.enabled)
        {
            return;
        }

        this.commonUtils.logInfo("Configuring game for bot spawning...");

        // Overwrite BSG's chances of bots being friendly toward each other
        this.botUtil.adjustAllBotHostilityChances();

        // Remove all of BSG's PvE-only boss waves
        this.botUtil.disablePvEBossWaves();

        // Currently these are all PMC waves, which are unnecessary with PMC spawns in this mod
        this.botUtil.disableCustomBossWaves();

        // Disable all of the extra Scavs that spawn into Factory
        this.botUtil.disableCustomScavWaves();

        // If Rogues don't spawn immediately, PMC spawns will be significantly delayed
        if (modConfig.bot_spawns.limit_initial_boss_spawns.disable_rogue_delay)
        {
            this.commonUtils.logInfo("Removing SPT Rogue spawn delay...");
            this.iLocationConfig.rogueLighthouseSpawnTimeSettings.waitTimeSeconds = -1;
        }

        if (modConfig.bot_spawns.advanced_eft_bot_count_management.enabled)
        {
            this.commonUtils.logInfo("Enabling advanced_eft_bot_count_management will instruct EFT to ignore this mod's PMC's and PScavs when spawning more bots.");
            this.botUtil.useEFTBotCaps();
            this.botUtil.modifyNonWaveBotSpawnSettings();
        }

        if (modConfig.bot_spawns.bot_cap_adjustments.enabled)
        {
            this.botUtil.increaseBotCaps();
        }
        
        this.commonUtils.logInfo("Configuring game for bot spawning...done.");
    }

    private async generateBots(info: IGenerateBotsRequestData, sessionID: string, shouldBePScavGroup: boolean) : Promise<IBotBase[]>
    {
        const bots = await this.botController.generate(sessionID, info);

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

    private doesFileIntegrityCheckPass(): boolean
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

        if (this.vfs.exists(`${path}/../../../BepInEx/plugins/SPTQuestingBots.dll`))
        {
            this.commonUtils.logError("Please remove BepInEx/plugins/SPTQuestingBots.dll from the previous version of this mod and restart the server, or it will NOT work correctly.");
        
            return false;
        }

        return true;
    }

    private shouldDisableSpawningSystem(importedModNames: string[]): boolean
    {
        if (!modConfig.bot_spawns.enabled)
        {
            return false;
        }
        
        for (const spawningModName of spawningModNames)
        {
            if (importedModNames.includes(spawningModName))
            {
                this.commonUtils.logWarning(`${spawningModName} detected. Disabling the Questing Bots spawning system.`);
                return true;
            }
        }

        return false;
    }
}

export interface IGenerateBotsRequestDataWithPScavChance
{
    conditions: ICondition[];
    PScavChance: number;
}

module.exports = { mod: new QuestingBots() }
