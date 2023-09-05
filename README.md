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
    * (15% Chance) Going to random spawn points on the map
    * (50% Chance) Rushing your spawn point if it's within 75m and within the first 30s of the raid (configurable)
    * (User-Specified Chance) Going to user-specified locations
* After reaching an objective or giving up, the bot will wait a configurable amount of time (currently 10s) before going to its next objective
* If a bot sees and enemy or was recently engaged in combat of any type, it will wait a configurable random amount of time (currently 10-30s) before resuming its quest
* Once a bot begins a quest, it will try to complete all objectives in it before moving to the next one
* Quests can be configured with the following parameters:
    * Min/Max player level
    * Max raid ET
    * Priority number
    * Chance of a bot selecting it
    * Min/Max distance from the bot for each objective
    * Max number of bots allowed per objective

**---------- Bot Quest-Selection Algorithm Overview ----------**
1) All quests are filtered to ensure they have at least one valid location on the map and the bot is able to accept the quest (is not blocked by player level, etc.)
2) Quests are grouped by priority number in ascending order
3) For each group, the following is performed:
    1) Distances from the bot to each quest objective are calculated
    2) Quests are sorted in ascending order based on the distance from the nearest objective to the bot, but randomness is added via **distance_randomness**, which is a percentage of the total range of distances for the objectives in the group
    3) A random number from 0-100 is selected and compared to the **chance** setting for the first quest in the group. If the number is less than the value of **chance**, the quest is assigned to the bot. 
4) If the bot isn't assigned a quest from any of the groups in step (3), it's assigned a random quest from the lowest-priority group.

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
* Some quest items or locations can't be resolved:
    * Fortress for Capturing Outposts in Customs
    * Scav Base for Capturing Outposts in Woods
    * Bronze pocket watch for Checking in Customs
    * Flash drive with fake info for Bullshit in Customs
    * Syringe with a chemical for Chemical Part 3 in Factory
    * Mountain Area for Return the Favor in Woods
    * The second and third bunkers for Assessment Part 2 in Woods
    * The satellite antenna in the USEC camp for Return the Favor in Woods
    * The USEC camp for Search Mission in Woods

**Planned Improvements for Bot Questing:**
* Adding an objective type for waiting a specified amount of time while patrolling the last objective area (for quests like Capturing Outposts)
* Adding a quest for hidden-stash-running with dynamically-generated objectives using the positions of all hidden stashes on the map
* Being able to invoke SAIN's logic for having bots extract from the map
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

**---------- Roadmap (Expect Similar Accuracy to EFT's) ----------**
* **0.2.0** (ETA: 9/5)
    * Implement support for basic custom quests (target locations only, no additional actions)
    * Implement min/max player levels for quests
    * Add extra quests to make bots go to other areas on the maps
    * Update bot quest-selection algorithm
    * Improve bot looting behavior
* **0.2.1** (ETA: 9/17)
    * **First version posted on SPT-AKI website**
    * Add documentation for config options and high-level overviews for how the algorithms work
    * Add more quests to make bots go to other areas on the maps
    * Bug fixes for 0.2.0 (hopefully none)
* **0.3.0** (ETA: Early October)
    * Rework quest data structures and logic layer to allow additional actions. Initially planned:
        * Patrol target area for a certain time
        * Wait at specific location for a certain time (mimicing planting items)
    * Implement quest-objective dependencies so certain objectives must be completed immediately before the next one (i.e. go to a specfic location and only then "plant" an item)
* **0.3.1** (ETA: Late October)
    * Add new quest-objective action: unlock doors
    * Add new quest type: hidden-stash running
    * Improve bot-spawn scheduling with initial PMC spawns to prevent them from getting "stuck in the queue" and not spawning until most of the Scavs die
    * Improve PMC senses to dissuade them from going to areas where many bots have died. Might require interaction with SAIN; TBD.
    * Initial PMC group spawns
* **Not Planned**
    * Add waypoints to have PMC's path around dangerous spots in the map or in very open areas