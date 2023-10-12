import { CommonUtils } from "./CommonUtils";

import { VFS } from "@spt-aki/utils/VFS";

export interface Quest
{
    repeatable: boolean,
    minLevel: number,
    maxLevel: number,
    chanceForSelecting: number,
    priority: number,
    maxRaidET: number,
    canRunBetweenObjectives: boolean,
    name: string,
    objectives: QuestObjective[]
}

export interface QuestObjective
{
    repeatable: boolean,
    maxBots: number,
    minDistanceFromBot: number,
    maxDistanceFromBot: number,
    maxRunDistance: number,
    steps: QuestObjectiveStep[]
}

export interface QuestObjectiveStep
{
    position : Vector3
}

export interface Vector3
{
    x: number,
    y: number,
    z: number
}

export class QuestManager
{
    constructor (private commonUtils: CommonUtils, private vfs: VFS)
    {

    }

    public validateCustomQuests() : void
    {
        const path = __dirname + "/../quests";

        // Ensure the directory for standard quests exists
        if (this.vfs.exists(path + "/standard/"))
        {
            const standardQuests = this.vfs.getFiles(path + "/standard/");
            for (const i in standardQuests)
            {
                const questFileText = this.vfs.readFile(path + "/standard/" + standardQuests[i]);

                // If the JSON file can be parsed into a Quest array, assume it's fine
                const quests : Quest[] = JSON.parse(questFileText);

                this.commonUtils.logInfo(`Found ${quests.length} standard quest(s) in "/standard/${standardQuests[i]}"`);
            }
        }
        else
        {
            this.commonUtils.logError("The \"/quests/standard/\" directory is missing.");
        }

        // Check if the directory for custom quests exists
        if (this.vfs.exists(path + "/custom/"))
        {
            const customQuests = this.vfs.getFiles(path + "/custom/");
            for (const i in customQuests)
            {
                const questFileText = this.vfs.readFile(path + "/custom/" + customQuests[i]);

                // If the JSON file can be parsed into a Quest array, assume it's fine
                const quests : Quest[] = JSON.parse(questFileText);

                this.commonUtils.logInfo(`Found ${quests.length} custom quest(s) in "/custom/${customQuests[i]}"`);
            }
        }
        else
        {
            this.commonUtils.logWarning("No custom quests found.");
        }
    }
}