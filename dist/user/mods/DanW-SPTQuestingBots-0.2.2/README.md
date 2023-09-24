**Please remember this is currently in ALPHA testing! Bugs are likely!**

**Requires:**
* BigBrain 0.2.0+
* Waypoints 1.2.0+

**Tested to be compatible with:**
* SAIN 2.0 Beta 3.5.3
* Looting Bots 1.1.2
* Late to the Party 1.3.0+ (if **initial_PMC_spawns=true** in this mod, set **adjust_bot_spawn_chances.enabled=false** in LTTP)
* SWAG + DONUTS 3.1.2 (if **initial_PMC_spawns=false** in this mod)
* Custom Raid Times 1.4.0
* Immersive Raids 1.1.3

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
* If a bot sees an enemy or was recently engaged in combat of any type, it will wait a configurable random amount of time (currently 10-30s) before resuming its quest
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
* Bots tend to get trapped in certain areas. Known areas:
    * Factory Gate 1 (will be fixed with next Waypoints release for SPT-AKI 3.7.0)
    * Customs between Warehouse 4 and New Gas
    * Lighthouse in the mountains near the Resort spawn
    * Lighthouse on the rocks near the helicopter crash
    * Lighthouse in various rocky areas
* Bots blindly run to their objective (unless they're in combat) even if it's certain death (i.e. running into the Sawmill when Shturman is there).
* Bots take the most direct path to their objectives, which may involve running in the middle of an open area without any cover.
* Certain bot "brains" stay in a combat state for a long time, during which they're unable to continue their quests.
* Certain bot "brains" are blacklisted because they cause the bot to always be in a combat state and therefore never quest (i.e. exUSEC's when they're near a stationary weapon)
* Some quest items or locations can't be resolved:
    * Fortress for Capturing Outposts in Customs
    * Scav Base for Capturing Outposts in Woods
    * Health Resort for Capturing Outposts in Shoreline
    * Bronze pocket watch for Checking in Customs
    * Flash drive with fake info for Bullshit in Customs
    * Syringe with a chemical for Chemical Part 3 in Factory
    * Mountain Area for Return the Favor in Woods
    * The second and third bunkers for Assessment Part 2 in Woods
    * The satellite antenna in the USEC camp for Return the Favor in Woods
    * The USEC camp for Search Mission in Woods
    * The cottage area for Overpopulation in Lighthouse
    * The main area for Assessment - Part 1 in Lighthouse
    * The bridge for Knock-Knock in Lighthouse
    * The truck for TerraGroup Trail Part 1 in Shoreline
    * The meeting spot for TerraGroup Trail Part 4 in Shoreline

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
* **0.2.2** (ETA: 9/17)
    * Add more quests to make bots go to other areas on the maps
    * Bug fixes
* **0.2.1** (ETA: 9/24)
    * **First version posted on SPT-AKI website**
    * Add documentation for config options and high-level overviews for how the algorithms work
* **0.3.0** (ETA: Mid October)
    * Rework quest data structures and logic layer to allow additional actions. Initially planned:
        * Patrol target area for a certain time
        * Wait at specific location for a certain time (mimicing planting items)
    * Implement quest-objective dependencies so certain objectives must be completed immediately before the next one (i.e. go to a specfic location and only then "plant" an item)
* **0.3.1** (ETA: Late October)
    * Add new quest-objective actions: unlocking doors and pulling levers
    * Add new quest type: hidden-stash running
    * Add new quest type: boss hunter
    * Add optional quest prerequisite to have at least one item in a list (i.e. a sniper rifle for sniping areas or an encoded DSP for Lighthouse)
    * Improve bot-spawn scheduling with initial PMC spawns to prevent them from getting "stuck in the queue" and not spawning until most of the Scavs die
    * Improve PMC senses to dissuade them from going to areas where many bots have died. Might require interaction with SAIN; TBD.
    * Initial PMC group spawns
* **Not Planned**
    * Add waypoints to have PMC's path around dangerous spots in the map or in very open areas

**---------- How to Add Custom Quests ----------**

To add custom quests to a map, first create a *user\mods\DanW-SPTQuestingBots-#.#.#\quests\custom* directory if it doesn't already exist. Then, create a file for each map for which you want to add custom quests. The file name should exactly match the corresponding file in the *user\mods\DanW-SPTQuestingBots-#.#.#\quests\standard* directory (case sensitive).

The three major data structures are:
* **Quests**: A quest is a collection of at least one quest objective, and objectives can be placed anywhere on the map.

    Quests have the following properties:
    * **repeatable**: Boolean value indicating if the bot can repeat the quest later in the raid. This is typically used for quests that are PvP or PvE focused, where a bot might want to check an area again later in the raid for more enemies.
    * **minLevel**: Only bots that are at least this player level will be allowed to select the quest
    * **maxLevel**: Only bots that are at most this player level will be allowed to select the quest
    * **chanceForSelecting**: The chance (in %) that the bot will accept the quest if the quest-selection algorithm selects it for the bot
    * **priority**: An integer indicating how the quest will be prioritized in the quest-selection algorithm. Quests that have a lower priority number are more likely to be selected.
    * **maxRaidET**: The quest can only be selected if this many seconds (or less) have elapsed in the raid. If you're using mods like **Late to the Party**, this is based on the overall raid time, not the time after you spawn. For example, if you set **maxRaidET=60** for a quest and you spawn into a Factory raid with 15 minutes remaining, this quest will never be used because 300 seconds has already elapsed in the overall raid. This property is typically used to make bots rush to locations like Dorms when the raid begins. 
    * **name**: The name of the quest. This doesn't have to be unique, but it's best to make it unique to avoid confusion when troubleshooting.
    * **objectives**: An array of the objectives in the quest. Bots can complete objectives in any order. 

* **Objectives**: An objective is a collection of at least one step. An objective represents a list of actions that the bot must complete. Currently, objectives only contain a list of positions that the bot needs to reach. In the future, an example objective could contain multiple types of steps such as: 1) Go to a door, 2) Unlock the door, 3) Go inside of the room.

    Quest objectives have the following properties:
    * **repeatable**: Boolean value indicating if the bot can repeat the quest objective later in the raid. This is typically used for quests are are PvP or PvE focused, where a bot might want to check an area again later in the raid for more enemies.
    * **maxBots**: The maximum number of bots that can actively be performing the objective.
    * **minDistanceFromBot**: The objective will only be selected if the bot is at least this many meters away from it.
    * **maxDistanceFromBot**: The objective will only be selected if the bot is no more than this many meters away from it.
    * **steps**: An array of the steps in the objective. Bots will complete the steps exactly in the order you specify.

* **Steps**: A step is an individual component of an objective. Currently, the only types of objective steps are going to a specific position.

    Quest objective steps have the following properties:
    * **position**: The position on the map that the bot will try to reach

**Tips and Tricks**
* Objectives should be sparsely placed on the map. Since bots take a break from questing after each objective is completed, they will wander around the area (for an unknown distance) before continuing the quest. If you place objective positions too close to each other, the bot will unnecessarily run back and forth around the area. As a rule of thumb, place objectives at least 20m from each other. 
* If you want a bot to go to several specific positions that are close to each other (i.e. adjacent rooms), use multiple steps in a single objectives instead of using multiple objectives. 
* Bots will use the NavMesh to calculate the more efficient path to their objective. They cannot perform complex actions to reach objective locations, so avoid placing objective steps on top of objects (i.e. inside truck beds) or in areas that are difficult to reach.