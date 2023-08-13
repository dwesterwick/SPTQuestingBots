import { CommonUtils } from "./CommonUtils";
import { IDatabaseTables } from "@spt-aki/models/spt/server/IDatabaseTables";
import { ITemplateItem } from "@spt-aki/models/eft/common/tables/ITemplateItem";

export class ItemHelper
{
    constructor(private commonUtils: CommonUtils, private databaseTables: IDatabaseTables)
    {
		
    }
	
    /**
     * Check if @param item is a child of any of the items with ID's @param parentIDs
     */
    public static hasAnyParents(item: ITemplateItem, parentIDs: string[], databaseTables: IDatabaseTables): boolean
    {
        for (const i in parentIDs)
        {
            if (ItemHelper.hasParent(item, parentIDs[i], databaseTables))
                return true;
        }
		
        return false;
    }
	
    /**
     * Check if @param item is a child of the item with ID @param parentID
     */
    public static hasParent(item: ITemplateItem, parentID: string, databaseTables: IDatabaseTables): boolean
    {
        const allParents = ItemHelper.getAllParents(item, databaseTables);
        return allParents.includes(parentID);
    }
	
    public static getAllParents(item: ITemplateItem, databaseTables: IDatabaseTables): string[]
    {
        if ((item._parent === null) || (item._parent === undefined) || (item._parent == ""))
            return [];
		
        const allParents = ItemHelper.getAllParents(databaseTables.templates.items[item._parent], databaseTables);
        allParents.push(item._parent);
		
        return allParents;
    }
}