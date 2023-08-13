import { CommonUtils } from "./CommonUtils";
import { ITraderConfig  } from "@spt-aki/models/spt/config/ITraderConfig";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { RagfairOfferGenerator } from "@spt-aki/generators/RagfairOfferGenerator";
import { RagfairOfferService } from "@spt-aki/services/RagfairOfferService";
import { RagfairServer } from "@spt-aki/servers/RagfairServer";
import { MemberCategory } from "@spt-aki/models/enums/MemberCategory"
import modConfig from "../config/config.json";

import { ItemHelper } from "./ItemHelper";
import { ITrader, ITraderAssort, IBarterScheme } from "@spt-aki/models/eft/common/tables/ITrader";

export class TraderAssortGenerator
{
    constructor
    (
        private commonUtils: CommonUtils,
        private traderConfig: ITraderConfig,
        private databaseTables: IDatabaseTables,
        private ragfairOfferGenerator: RagfairOfferGenerator,
        private ragfairServer: RagfairServer,
        private ragfairOfferService: RagfairOfferService
    )
    {
		
    }
	
    public static rebuildArray(array: any[]): any[]
    {
        const newArray = [];
        for (const i in array)
        {
            if ((array[i] !== null) && (array[i] !== undefined))
                newArray.push(array[i]);			
        }		
        return newArray;
    }
	
    public disableFence(): void
    {
        this.commonUtils.logInfo("Disabling Fence...");		
        this.traderConfig.fence.assortSize = 0;
        this.traderConfig.fence.discountOptions.assortSize = 0;
    }
	
    public refreshRagfairOffers(): void
    {
        this.commonUtils.logInfo("Refreshing Ragfair offers...");		
        this.ragfairOfferGenerator.generateDynamicOffers();
		
        // getUpdateableTraders() is protected but seems to work. I build this list manually just to be safe. 
        //const updateableTraders = this.ragfairServer.getUpdateableTraders();

        const updateableTraders = [];
        const offers = this.ragfairOfferService.getOffers();

        // Review all offers and generate an array of ID's for traders who appear in them
        for (const offer in offers)
        {
            if (offers[offer].user.memberType == MemberCategory.TRADER)
                if (!updateableTraders.includes(offers[offer].user.id))
                    updateableTraders.push(offers[offer].user.id);
        }

        if (modConfig.services.flea_market.only_barter_offers)
        {
            this.commonUtils.logInfo("Removing cash offers from players...");
			
            for (const offer in offers)
            {
                if (!TraderAssortGenerator.isABarterOffer(offers[offer].requirements, this.databaseTables))
                    this.ragfairOfferService.removeOfferById(offers[offer]._id);
            }
        }

        for (const i in updateableTraders)
        {
            this.commonUtils.logInfo(`Refreshing Ragfair offers for ${this.commonUtils.getTraderName(updateableTraders[i])}...`);
            this.ragfairOfferGenerator.generateFleaOffersForTrader(updateableTraders[i]);
        }
    }
	
    public updateTraderAssorts(): void
    {
        this.commonUtils.logInfo("Updating trader assorts...");
		
        for (const trader in this.databaseTables.traders)
        {
            const assort = this.databaseTables.traders[trader].assort;

            // Ignore traders who don't sell anything (i.e. Lightkeeper)
            if ((assort === null) || (assort === undefined))
                continue;
			
            this.commonUtils.logInfo(`Updating trader assorts for ${this.commonUtils.getTraderName(trader)}...`);
			
            const newAssort = TraderAssortGenerator.getNewTraderAssort(this.databaseTables.traders[trader], this.databaseTables);

            // need to do this or generateFleaOffersForTrader() will complain about undefined slotID's.
            this.databaseTables.traders[trader].assort.items = TraderAssortGenerator.rebuildArray(newAssort.items);
        }
    }
	
    private static getNewTraderAssort(trader: ITrader, databaseTables: IDatabaseTables): ITraderAssort
    {
        let lastItemRemoved = false;
        for (const assortItem in trader.assort.items)
        {
            const barterID = trader.assort.items[assortItem]._id;
			
            // Band-aid fix for missing slotId for Jaeger assort item 634acfb4da5c23324e07ca36 in 3.5.1 release. Probably needs to stay because this mod
            // needs to remain compatible with that version.
            if ((trader.assort.items[assortItem].slotId === null) || (trader.assort.items[assortItem].slotId === undefined))
            {
                if (trader.assort.items[assortItem]._id == "634acfb4da5c23324e07ca36")
                    trader.assort.items[assortItem].slotId = "hideout";
            }

            // If slotId is "hideout", the item appears in the grid of trader offers and is not a child of another item in the grid
            if (trader.assort.items[assortItem].slotId.toLowerCase() != "hideout")
            {
                // ASSuming child items appear immediately afterward in the assort item array, they should be removed if the parent item is removed
                if (lastItemRemoved)
                    this.removeItemFromTraderAssort(assortItem, barterID, trader);
				
                continue;
            }
			
            lastItemRemoved = false;
			
            const itemID = trader.assort.items[assortItem]._tpl;
            const item = databaseTables.templates.items[itemID];
			
            if (item._props.QuestItem == true)
                continue;
            
            if (modConfig.traders.whitelist.items.includes(itemID))
                continue;
            
            if (ItemHelper.hasAnyParents(item, modConfig.traders.whitelist.parents, databaseTables))
                continue;
            
            if (!modConfig.traders.whitelist_only && (!modConfig.traders.barters_only || TraderAssortGenerator.isABarterOffer(trader.assort.barter_scheme[barterID][0], databaseTables)))
                continue;
			
            this.removeItemFromTraderAssort(assortItem, barterID, trader);
            lastItemRemoved = true;
        }
		
        return trader.assort;
    }
	
    public static isABarterOffer(barterScheme: IBarterScheme[], databaseTables: IDatabaseTables): boolean
    {
        for (const buyReq in barterScheme)
        {
            const buyReqItem = databaseTables.templates.items[barterScheme[buyReq]._tpl];
			
            if (buyReqItem._parent != modConfig.traders.ID_money)
                return true;
        }
		
        return false;
    }
	
    private static removeItemFromTraderAssort(assortItemID: string, barterID: string, trader: ITrader): void
    {
        delete trader.assort.loyal_level_items[barterID];
        delete trader.assort.barter_scheme[barterID];

        delete trader.assort.items[assortItemID];
        
        if (trader.questassort === undefined)
            return;
        
        if (barterID in trader.questassort.started)
            delete trader.questassort.started[barterID];
        if (barterID in trader.questassort.success)
            delete trader.questassort.success[barterID];
        if (barterID in trader.questassort.fail)
            delete trader.questassort.fail[barterID];
    }
}