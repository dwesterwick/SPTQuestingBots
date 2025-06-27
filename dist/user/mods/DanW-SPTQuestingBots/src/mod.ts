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
import type { FileSystemSync } from "@spt/utils/FileSystemSync";
import type { HttpResponseUtil } from "@spt/utils/HttpResponseUtil";
import type { RandomUtil } from "@spt/utils/RandomUtil";
import type { WeightedRandomHelper } from "@spt/helpers/WeightedRandomHelper";
import type { BotController } from "@spt/controllers/BotController";
import type { BotNameService } from "@spt/services/BotNameService";
import type { BotCallbacks } from "@spt/callbacks/BotCallbacks";
import type { IGenerateBotsRequestData, ICondition } from "@spt/models/eft/bot/IGenerateBotsRequestData";
import type { IBotBase, IInfo } from "@spt/models/eft/common/tables/IBotBase";

import { ConfigTypes } from "@spt/models/enums/ConfigTypes";
import { GameEditions } from "@spt/models/enums/GameEditions";
import { MemberCategory } from "@spt/models/enums/MemberCategory";
import type { IBotConfig } from "@spt/models/spt/config/IBotConfig";
import type { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";
import type { ILocationConfig } from "@spt/models/spt/config/ILocationConfig";

const modName = "SPTQuestingBots";
const spawningModNames = ["SWAG", "DewardianDev-MOAR", "PreyToLive-BetterSpawnsPlus", "RealPlayerSpawn", "acidphantasm-botplacementsystem"];

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
    private fileSystem: FileSystemSync;
    private httpResponseUtil: HttpResponseUtil;
    private randomUtil: RandomUtil;
    private weightedRandomHelper: WeightedRandomHelper;
    private botController: BotController;
    private botNameService: BotNameService;
    private iBotConfig: IBotConfig;
    private iPmcConfig: IPmcConfig;
    private iLocationConfig: ILocationConfig;

    private basePScavConversionChance: number;

    public preSptLoad(container: DependencyContainer): void 
    {
        this.logger = container.resolve<ILogger>("WinstonLogger");
        const staticRouterModService = container.resolve<StaticRouterModService>("StaticRouterModService");
        const dynamicRouterModService = container.resolve<DynamicRouterModService>("DynamicRouterModService");
		
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
            result.generateBots = async (url: string, info: IGenerateBotsRequestDataWithPScavForced, sessionID: string) =>
            {
                const bots = await this.generateBots({ conditions: info.conditions }, sessionID, info.GeneratePScav);
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
        this.fileSystem = container.resolve<FileSystemSync>("FileSystemSync");
        this.httpResponseUtil = container.resolve<HttpResponseUtil>("HttpResponseUtil");
        this.randomUtil = container.resolve<RandomUtil>("RandomUtil");
        this.weightedRandomHelper = container.resolve<WeightedRandomHelper>("WeightedRandomHelper");
        this.botController = container.resolve<BotController>("BotController");
        this.botNameService = container.resolve<BotNameService>("BotNameService");

        this.iBotConfig = this.configServer.getConfig(ConfigTypes.BOT);
        this.iPmcConfig = this.configServer.getConfig(ConfigTypes.PMC);
        this.iLocationConfig = this.configServer.getConfig(ConfigTypes.LOCATION);

        this.databaseTables = this.databaseServer.getTables();
        this.commonUtils = new CommonUtils(this.logger, this.databaseTables, this.localeService);
        this.botUtil = new BotUtil(this.commonUtils, this.databaseTables, this.iLocationConfig, this.iBotConfig, this.iPmcConfig);
        this.pmcConversionUtil = new PMCConversionUtil(this.commonUtils, this.iPmcConfig, this.iBotConfig);

        if (!modConfig.enabled)
        {
            return;
        }

        if (!this.doesFileIntegrityCheckPass())
        {
            modConfig.enabled = false;
            return;
        }

        if (!this.areArraysValid())
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
        this.botUtil.disableBotWaves(this.iLocationConfig.customWaves.boss, "boss")

        // Disable all of the extra Scavs that spawn into Factory
        this.botUtil.disableBotWaves(this.iLocationConfig.customWaves.normal, "Scav");

        // Disable SPT's PMC wave generator
        this.botUtil.disableBotWaves(this.iPmcConfig.customPmcWaves, "PMC");

        // Use EFT's bot caps instead of SPT's
        this.botUtil.useEFTBotCaps();

        // If Rogues don't spawn immediately, PMC spawns will be significantly delayed
        if (modConfig.bot_spawns.limit_initial_boss_spawns.disable_rogue_delay && (this.iLocationConfig.rogueLighthouseSpawnTimeSettings.waitTimeSeconds > -1))
        {
            this.iLocationConfig.rogueLighthouseSpawnTimeSettings.waitTimeSeconds = -1;
            this.commonUtils.logInfo("Removed SPT Rogue spawn delay");
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

        for (const bot in bots)
        {
            if (bots[bot].Info.Settings.Role !== "assault")
            {
                this.commonUtils.logDebug(`Tried generating a player Scav, but a bot with role ${bots[bot].Info.Settings.Role} was returned`);
                continue;
            }

            this.botNameService.addRandomPmcNameToBotMainProfileNicknameProperty(bots[bot]);
            this.setRandomisedGameVersionAndCategory(bots[bot].Info);
        }

        return bots;
    }

    private setRandomisedGameVersionAndCategory(botInfo : IInfo) : string
    {
        /* SPT CODE - BotGenerator.setRandomisedGameVersionAndCategory(bot.Info) */

        // Special case
        if (botInfo.Nickname?.toLowerCase() === "nikita")
        {
            botInfo.GameVersion = GameEditions.UNHEARD;
            botInfo.MemberCategory = MemberCategory.DEVELOPER;

            return botInfo.GameVersion;
        }

        // Choose random weighted game version for bot
        botInfo.GameVersion = this.weightedRandomHelper.getWeightedValue(this.iPmcConfig.gameVersionWeight);

        // Choose appropriate member category value
        switch (botInfo.GameVersion) 
        {
            case GameEditions.EDGE_OF_DARKNESS:
                botInfo.MemberCategory = MemberCategory.UNIQUE_ID;
                break;
            case GameEditions.UNHEARD:
                botInfo.MemberCategory = MemberCategory.UNHEARD;
                break;
            default:
                // Everyone else gets a weighted randomised category
                botInfo.MemberCategory = Number.parseInt(
                    this.weightedRandomHelper.getWeightedValue(this.iPmcConfig.accountTypeWeight),
                    10
                );
        }

        // Ensure selected category matches
        botInfo.SelectedMemberCategory = botInfo.MemberCategory;

        return botInfo.GameVersion;
    }

    private doesFileIntegrityCheckPass(): boolean
    {
        const path = `${__dirname}/..`;

        if (this.fileSystem.exists(`${path}/quests/`))
        {
            this.commonUtils.logWarning("Found obsolete quests folder 'user\\mods\\DanW-SPTQuestingBots\\quests'. Only quest files in 'BepInEx\\plugins\\DanW-SPTQuestingBots\\quests' will be used.");
        }

        if (this.fileSystem.exists(`${path}/log/`))
        {
            this.commonUtils.logWarning("Found obsolete log folder 'user\\mods\\DanW-SPTQuestingBots\\log'. Logs are now saved in 'BepInEx\\plugins\\DanW-SPTQuestingBots\\log'.");
        }

        if (this.fileSystem.exists(`${path}/../../../BepInEx/plugins/SPTQuestingBots.dll`))
        {
            this.commonUtils.logError("Please remove BepInEx/plugins/SPTQuestingBots.dll from the previous version of this mod and restart the server, or it will NOT work correctly.");
        
            return false;
        }

        return true;
    }

    private areArraysValid(): boolean
    {
        if (!this.isChanceArrayValid(modConfig.questing.bot_quests.eft_quests.level_range, true))
        {
            this.commonUtils.logError("questing.bot_quests.eft_quests.level_range has invalid data. Mod disabled.")
            return false;
        }

        if (!this.isChanceArrayValid(modConfig.bot_spawns.pmcs.fraction_of_max_players_vs_raidET, false))
        {
            this.commonUtils.logError("bot_spawns.pmcs.fraction_of_max_players_vs_raidET has invalid data. Mod disabled.")
            return false;
        }

        if (!this.isChanceArrayValid(modConfig.bot_spawns.pmcs.bots_per_group_distribution, true))
        {
            this.commonUtils.logError("bot_spawns.pmcs.bots_per_group_distribution has invalid data. Mod disabled.")
            return false;
        }
        if (!this.isChanceArrayValid(modConfig.bot_spawns.pmcs.bot_difficulty_as_online, true))
        {
            this.commonUtils.logError("bot_spawns.pmcs.bot_difficulty_as_online has invalid data. Mod disabled.")
            return false;
        }
        if (!this.isChanceArrayValid(modConfig.bot_spawns.player_scavs.bots_per_group_distribution, true))
        {
            this.commonUtils.logError("bot_spawns.player_scavs.bots_per_group_distribution has invalid data. Mod disabled.")
            return false;
        }
        if (!this.isChanceArrayValid(modConfig.bot_spawns.player_scavs.bot_difficulty_as_online, true))
        {
            this.commonUtils.logError("bot_spawns.player_scavs.bot_difficulty_as_online has invalid data. Mod disabled.")
            return false;
        }

        if (!this.isChanceArrayValid(modConfig.adjust_pscav_chance.chance_vs_time_remaining_fraction, false))
        {
            this.commonUtils.logError("adjust_pscav_chance.chance_vs_time_remaining_fraction has invalid data. Mod disabled.")
            return false;
        }

        return true;
    }

    private isChanceArrayValid(array: number[][], shouldLeftColumnBeIntegers: boolean): boolean
    {
        if (array.length === 0)
        {
            return false;
        }

        for (const row of array)
        {
            if (row.length !== 2)
            {
                return false;
            }

            if (shouldLeftColumnBeIntegers && !Number.isInteger(row[0]))
            {
                this.commonUtils.logError("Found a chance array with an invalid value in its left column. Please ensure you are not using an outdated version of config.json.");

                return false;
            }
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

export interface IGenerateBotsRequestDataWithPScavForced
{
    conditions: ICondition[];
    GeneratePScav: boolean;
}

module.exports = { mod: new QuestingBots() }
