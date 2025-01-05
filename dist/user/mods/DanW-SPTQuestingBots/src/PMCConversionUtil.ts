import modConfig from "../config/config.json";

import type { CommonUtils } from "./CommonUtils";
import type { MinMax } from "@spt/models/common/MinMax";
import type { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";

export class PMCConversionUtil
{
    private convertIntoPmcChanceOrig: Record<string, Record<string, MinMax>> = {};

    constructor(private commonUtils: CommonUtils, private iPmcConfig: IPmcConfig)
    {
        
    }

    public setAllOriginalPMCConversionChances(): void
    {
        // Store the default PMC-conversion chances for each bot type defined in SPT's configuration file
        let logMessage = "";
        for (const map in this.iPmcConfig.convertIntoPmcChance)
        {
            logMessage += `${map} = [`;

            for (const pmcType in this.iPmcConfig.convertIntoPmcChance[map])
            {
                if ((this.convertIntoPmcChanceOrig[map] !== undefined) && (this.convertIntoPmcChanceOrig[map][pmcType] !== undefined))
                {
                    logMessage += `${pmcType}: already buffered, `;
                    continue;
                }

                this.setOriginalPMCConversionChances(map, pmcType);

                const chances = this.convertIntoPmcChanceOrig[map][pmcType];
                logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
            }

            logMessage += "], ";
        }

        this.commonUtils.logInfo(`Reading default PMC spawn chances: ${logMessage}`);
    }

    private setOriginalPMCConversionChances(map: string, pmcType: string): void
    {
        const chances: MinMax = {
            min: this.iPmcConfig.convertIntoPmcChance[map][pmcType].min,
            max: this.iPmcConfig.convertIntoPmcChance[map][pmcType].max
        }

        if (this.convertIntoPmcChanceOrig[map] === undefined)
        {
            this.convertIntoPmcChanceOrig[map] = {};
        }

        this.convertIntoPmcChanceOrig[map][pmcType] = chances;
    }

    public adjustAllPmcConversionChances(scalingFactor: number, verify: boolean): void
    {
        // Adjust the chances for each applicable bot type
        let logMessage = "";
        let verified = true;
        for (const map in this.iPmcConfig.convertIntoPmcChance)
        {
            logMessage += `${map} = [`;

            for (const pmcType in this.iPmcConfig.convertIntoPmcChance[map])
            {
                verified = verified && this.adjustAndVerifyPmcConversionChances(map, pmcType, scalingFactor, verify);

                const chances = this.iPmcConfig.convertIntoPmcChance[map][pmcType];
                logMessage += `${pmcType}: ${chances.min}-${chances.max}%, `;
            }

            logMessage += "], ";

            if (!verified)
            {
                break;
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

    public adjustAndVerifyPmcConversionChances(map: string, pmcType: string, scalingFactor: number, verify: boolean): boolean
    {
        // Do not allow the chances to exceed 100%. Who knows what might happen...
        const min = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[map][pmcType].min * scalingFactor));
        const max = Math.round(Math.min(100, this.convertIntoPmcChanceOrig[map][pmcType].max * scalingFactor));
        
        if (verify)
        {
            if (this.iPmcConfig.convertIntoPmcChance[map][pmcType].min !== min)
            {
                return false;
            }

            if (this.iPmcConfig.convertIntoPmcChance[map][pmcType].max !== max)
            {
                return false;
            }
        }
        
        this.iPmcConfig.convertIntoPmcChance[map][pmcType].min = min;
        this.iPmcConfig.convertIntoPmcChance[map][pmcType].max = max;

        return true;
    }

    public removeBlacklistedBrainTypes(): void
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
}