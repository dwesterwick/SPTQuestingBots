import { ILogger } from "@spt-aki/models/spt/utils/ILogger";

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
    constructor (private logger: ILogger)
    {

    }
}