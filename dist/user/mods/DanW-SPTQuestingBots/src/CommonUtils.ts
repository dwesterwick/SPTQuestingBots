import modConfig from "../config/config.json";

import type { ILogger } from "@spt-aki/models/spt/utils/ILogger";
import type { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import type { LocaleService } from "@spt-aki/services/LocaleService";

export class CommonUtils
{
    private debugMessagePrefix = "[Questing Bots] ";
    private translations: Record<string, string>;
	
    constructor (private logger: ILogger, private databaseTables: IDatabaseTables, private localeService: LocaleService)
    {
        // Get all translations for the current locale
        this.translations = this.localeService.getLocaleDb();
    }
	
    public logInfo(message: string, alwaysShow = false): void
    {
        if (modConfig.enabled || alwaysShow)
            this.logger.info(this.debugMessagePrefix + message);
    }

    public logWarning(message: string): void
    {
        this.logger.warning(this.debugMessagePrefix + message);
    }

    public logError(message: string): void
    {
        this.logger.error(this.debugMessagePrefix + message);
    }

    public getItemName(itemID: string): string
    {
        const translationKey = `${itemID} Name`;
        if (translationKey in this.translations)
            return this.translations[translationKey];
		
        // If a key can't be found in the translations dictionary, fall back to the template data if possible
        if (!(itemID in this.databaseTables.templates.items))
        {
            return undefined;
        }

        const item = this.databaseTables.templates.items[itemID];
        return item._name;
    }
}