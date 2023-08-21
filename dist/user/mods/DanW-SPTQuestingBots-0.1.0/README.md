**Please remember this is currently in ALPHA testing! Bugs are likely!**

**Requires:**
* BigBrain 0.2.0+
* Waypoints 1.2.0+

**Tested to be compatible with:**
* SAIN 2.0 Beta 3.5.3
* Looting Bots 1.1.2
* Late to the Party 1.3.0+ (if **initial_PMC_spawns=true** in this mod, set **adjust_bot_spawn_chances.enabled=false** in LTTP)
* SWAG + DONUTS 3.1.2 (if **initial_PMC_spawns=false** in this mod)

**NOT compatible with:**
* AI Limit / AI Disabler (this mod relies on the AI being active throughout the entire map)

**---------- Bot Questing Overview ----------**
* All PMC bots will automatically select random quests, regardless of their spawn time
* There are currently three types of quests available:
    * (50% Chance) Zones and item locations for actual EFT quests (these are dynamically generated using the quest-data from the server, so it will also work with mods that add or change quests)
    * (30% Chance) Going to random spawn points on the map
    * (50% Chance) Rushing your spawn point if it's within 75m and within the first 30s of the raid (configurable)
* After reaching an objective or giving up, the bot will wait a configurable amount of time (currently 10s) before going to its next objective
* If a bot sees and enemy or was recently engaged in combat of any type, it will wait a configurable random amount of time (currently 10-30s) before resuming its quest

**Known Issues for Bot Questing:**
* Bots can't tell if a locked door is blocking their path and will give up instead of unlocking it
* Bots tend to get trapped in certain spawn areas. Known areas:
    * Factory Gate 1 (will be fixed with next Waypoints release for SPT-AKI 3.7.0)
    * Customs between Warehouse 4 and New Gas
    * Lighthouse in the mountains near the Resort spawn
    * Lighthouse on the rocks near the helicopter crash
* Bots blindly run to their objective (unless they're in combat) even if it's certain death (i.e. running into the Sawmill when Shturman is there).
* Bots take the most direct path to their objectives, which may involve running in the middle of an open area without any cover.
* Certain bot "brains" stay in a combat state for a long time, during which they're unable to continue their quests.
* Certain bot "brains" are blacklisted because they cause the bot to always be in a combat state and therefore never quest (i.e. exUSEC's when they're near a stationary weapon)

**Planned Improvements for Bot Questing:**
* Custom quests (defined via JSON files)
* Specifying the order in which bots should go to quest objectives
* Adding an objective type for waiting a specified amount of time while patrolling the last objective area (for quests like Capturing Outposts)
* Adding a quest for hidden-stash-running with dynamically-generated objectives using the positions of all hidden stashes on the map
* Being able to invoke SAIN's logic for having bots extract from the map
* Adding min/max player levels for quests, so bots will only perform them if their level is reasonable
* Optionally overriding the default priority for EFT quests to make bots more or less likely to select them

**---------- Initial PMC Spawning Overview ----------**
* PMC's will spawn at the beginning of the raid (Thanks to Props for sharing code from DONUTS!). However, all initial bosses must spawn first except for Factory. 
* The number of initial PMC's is a random number between the min and max player count for the map (other mods can change these values).
* Only a certain (configurable) number of initial PMC's will spawn at the beginning of the raid, and the rest will spawn as the existing ones die.
* Initial PMC's will spawn in actual EFT player spawn points using an algorithm to spread bots out across the map as much as possible. PMC's that spawn after the initial wave can spawn anywhere that is far enough from you and other bots.
* The PMC difficulty is set by your raid settings in EFT
* To accomodate the large initial PMC wave and still allow Scavs and bosses to spawn, the max-bot cap is (optionally) increased. 

**Known Issues for Initial PMC Spawning:**
* Only solo PMC's spawn
* If there is a lot of PMC action at the beginning of the raid, the rest of the raid will feel dead. However, this isn't so different from live Tarkov. 
* In maps with a high number of max players, Scavs don't always spawn when the game generates them if your **max_alive_initial_pmcs** setting is high
* In maps with a high number of max players, performance can be pretty bad if your **max_alive_initial_pmcs** or **max_total_bots** settings are high
* Noticeable stuttering for (possibly) several seconds when the initial PMC wave spawns if your **max_alive_initial_pmcs** setting is high

**Planned Improvements for Initial PMC Spawning:**
* Add random PMC group spawns