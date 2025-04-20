import modConfig from "../config/config.json";

import type { CommonUtils } from "./CommonUtils";
import type { IPmcConfig } from "@spt/models/spt/config/IPmcConfig";
import type { IBotConfig } from "@spt/models/spt/config/IBotConfig";

export class PMCConversionUtil
{
    constructor(private commonUtils: CommonUtils, private iPmcConfig: IPmcConfig, private iBotConfig: IBotConfig)
    {
        
    }

    public removeBlacklistedBrainTypes(): void
    {
        const badBrains = modConfig.bot_spawns.blacklisted_pmc_bot_brains;

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

                    // this.commonUtils.logInfo(`Removing ${badBrains[i]} from ${pmcType} in ${map}...`);
                    delete mapBrains[badBrains[i]];
                    removedBrains++;
                }
            }
        }

        for (const map in this.iBotConfig.playerScavBrainType)
        {
            const mapBrains = this.iBotConfig.playerScavBrainType[map];
            
            for (const i in badBrains)
            {
                if (mapBrains[badBrains[i]] === undefined)
                {
                    continue;
                }

                // this.commonUtils.logInfo(`Removing ${badBrains[i]} from playerscavs in ${map}...`);
                delete mapBrains[badBrains[i]];
                removedBrains++;
            }
        }

        this.commonUtils.logInfo(`Removed ${removedBrains} blacklisted brain types from being used for PMC's and Player Scav's`);
    }
}