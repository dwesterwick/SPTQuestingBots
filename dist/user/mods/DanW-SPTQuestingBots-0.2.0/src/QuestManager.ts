import { CommonUtils } from "./CommonUtils";

import { VFS } from "@spt-aki/utils/VFS";

export interface Quest
{
    minLevel: number,
    maxLevel: number,
    chanceForSelecting: number,
    priority: number,
    name: string,
    objectives: QuestObjective[]
}

export interface QuestObjective
{
    maxBots: number,
    minDistanceFromBot: number,
    maxDistanceFromBot: number,
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

    public getCustomQuests(locationID: string) : Quest[]
    {
        const standardQuestFile = __dirname + "/../quests/standard/" + locationID + ".json";
        const customQuestFile = __dirname + "/../quests/custom/" + locationID + ".json";
        let quests: Quest[] = [];

        if (this.vfs.exists(standardQuestFile))
        {
            this.commonUtils.logInfo(`Loading standard quests for ${locationID}...`);

            const questFileText = this.vfs.readFile(standardQuestFile);
            const questData : Quest[] = JSON.parse(questFileText)
            quests = quests.concat(questData);
        }
        else
        {
            this.commonUtils.logWarning(`No standard quests found for ${locationID}`);
        }

        if (this.vfs.exists(customQuestFile))
        {
            this.commonUtils.logInfo(`Loading custom quests for ${locationID}...`);

            const questFileText = this.vfs.readFile(customQuestFile);
            const questData : Quest[] = JSON.parse(questFileText)
            quests = quests.concat(questData);
        }

        this.commonUtils.logInfo(`Loaded ${quests.length} quests for ${locationID}.`);
        return quests;
    }
}