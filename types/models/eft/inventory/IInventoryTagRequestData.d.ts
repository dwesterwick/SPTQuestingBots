import { IInventoryBaseActionRequestData } from "@spt/models/eft/inventory/IInventoryBaseActionRequestData";
export interface IInventoryTagRequestData extends IInventoryBaseActionRequestData {
    Action: "Tag";
    item: string;
    TagName: string;
    TagColor: number;
}
