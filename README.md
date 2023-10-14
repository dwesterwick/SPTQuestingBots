You're no longer the only PMC running around placing markers and collecting quest items. The bots have transcended and are coming for you...

**This is a work-in-progress! Many features are still in development. Please report bugs in the QuestingBots thread in Discord.**

**---------- Mod Compatibility ----------**

**REQUIRES:**
* [BigBrain](https://hub.sp-tarkov.com/files/file/1219-bigbrain/)
* Waypoints

**NOT compatible with:**
* AI Limit / AI Disabler (this mod relies on the AI being active throughout the entire map)

**Highly Recommended:**
* SAIN
* Looting Bots

**Compatible with:**
* SWAG + DONUTS (if **initial_PMC_spawns=false** in this mod)
* Late to the Party (if **initial_PMC_spawns=true** in this mod, set **adjust_bot_spawn_chances.enabled=false** in LTTP)

**Previously compatible with (but not confirmed in SPT-AKI 3.7.0+):**
* Looting Bots
* Custom Raid Times
* Immersive Raids

**---------- Overview ----------**

There are two main components of this mod: adding an objective system to the AI and spawning PMC's only at the beginning of the raid to mimic live Tarkov.

**Objective System:**
Instead of patrolling their spawn areas, bots will now move around the map to perform randomly-selected objectives. By default this system is only active for PMC's, but it can be enabled for Scavs and bosses if you want an extra challenge.

After spawning (regardless of when this occurs during the raid), bots will immediately begin questing, and there are only a few conditions that will cause them to stop questing:
* They got stuck too many times
* They're overencumbered
* They're trying to extract (using SAIN)

Otherwise, they will only temporarily stop questing for the following reasons:
* They're currently or were just recently in combat
* They recently completed an objective
* They're checking for or have found loot
* Their health is too low or they have blacked limbs (except arms)
* Their energy or hydration is too low
* They have followers that are too far from them

There are currently several types of quests available to every bot:
* **EFT Quests:** Bots will go to locations specified in EFT quests for placing markers, collecting/placing items, killing bots, etc. Bots can also use quests added by other mods. 
* **Spawn Rush**: At the beginning of the raid, bots that are within a certain distance of you will run to your spawn point.
* **Spawn Point Wandering**: Bots will wander to different spawn points around the map
* **"Standard" Quests:**: Bots will go to specified locations around the map. They will prioritize more desirable locations for loot and locations that are close to them. 
* **"Custom" Quests:** You can create your own quests for bots using the templates for "standard" quests.

**PMC Spawning System:**
At the beginning of the raid, PMC's will spawn around the map at actual EFT spawn points. The spawning system will try to separate spawn points as much as possible, but spawn killing is still entirely possible just like it is in live Tarkov. The number of initial PMC's is a random number between the min and max player count for the map (other mods can change these values).

Only a certain (configurable) number of initial PMC's will spawn at the beginning of the raid, and the rest will spawn as the existing ones die. PMC's that spawn after the initial wave can spawn anywhere that is far enough from you and other bots (not just at EFT PMC spawn points). All initial bosses must spawn first (except for Factory) or EFT may suppress them due to the high number of bots on the map. The PMC difficulty is set by your raid settings in EFT.

To accomodate the large initial PMC wave and still allow Scavs and bosses to spawn, the max-bot cap is (optionally) increased.

**NOTE: Please disable the PMC-spawning system in this mod if you're using other mods like SWAG/DONUTS that manage spawning! Otherwise, there will be too many PMC's on the map.**

**---------- Bot Quest-Selection Algorithm Overview ----------**

1) All quests are filtered to ensure they have at least one valid location on the map and the bot is able to accept the quest (is not blocked by player level, etc.)
2) Quests are grouped by priority number in ascending order
3) For each group, the following is performed:
    1) Distances from the bot to each quest objective are calculated
    2) Quests are sorted in ascending order based on the distance from the nearest objective to the bot, but randomness is added via **distance_randomness**, which is a percentage of the total range of distances for the objectives in the group
    3) A random number from 0-100 is selected and compared to the **chance** setting for the first quest in the group. If the number is less than the value of **chance**, the quest is assigned to the bot. 
4) If the bot isn't assigned a quest from any of the groups in step (3), it's assigned a random quest from the lowest-priority group.

**---------- Known Issues ----------**

**Objective System:**
* Mods that add a lot of new quests may cause latency issues that may result in game stability problems
* Bots can't tell if a locked door is blocking their path and will give up instead of unlocking it
* Bots tend to get trapped in certain areas. Known areas:
    * Factory Gate 1 (will be fixed with next Waypoints release for SPT-AKI 3.7.0) <-- need to test this again
    * Customs between Warehouse 4 and New Gas
    * Lighthouse in the mountains near the Resort spawn
    * Lighthouse on the rocks near the helicopter crash
    * Lighthouse in various rocky areas
* Bots blindly run to their objective (unless they're in combat) even if it's certain death (i.e. running into the Sawmill when Shturman is there). They will only engage you if they see you, so they may run right past you. 
* Bots take the most direct path to their objectives, which may involve running in the middle of an open area without any cover.
* Certain bot "brains" stay in a combat state for a long time, during which they're unable to continue their quests.
* Certain bot "brains" are blacklisted because they cause the bot to always be in a combat state and therefore never quest (i.e. exUSEC's when they're near a stationary weapon)
* Some quest items or locations can't be resolved:
    * Fortress for Capturing Outposts in Customs
    * Scav Base for Capturing Outposts in Woods
    * Health Resort for Capturing Outposts in Shoreline
    * Bronze pocket watch for Checking in Customs
    * Flash drive with fake info for Bullshit in Customs
    * Mountain Area for Return the Favor in Woods
    * The second and third bunkers for Assessment Part 2 in Woods
    * The satellite antenna in the USEC camp for Return the Favor in Woods
    * The USEC camp for Search Mission in Woods
    * The cottage area for Overpopulation in Lighthouse
    * The main area for Assessment - Part 1 in Lighthouse
    * The bridge for Knock-Knock in Lighthouse
    * All locations for Long Line in Interchange
    * The 21WS Container for Provocation in Interchange
    * The underground depot for Safe Corridor in Reserve
    * One of the locations for Bunker Part 2 in Reserve (not sure which)

**PMC Spawning System:**
* Only solo PMC's spawn
* Not all PMC's spawn into Streets because too many Scavs spawn into the map first
* Scavs can spawn close to PMC's (SPT limitation)
* If there is a lot of PMC action at the beginning of the raid, the rest of the raid will feel dead. However, this isn't so different from live Tarkov. 
* In maps with a high number of max players, Scavs don't always spawn when the game generates them if your **max_alive_initial_pmcs** setting is high
* In maps with a high number of max players, performance can be pretty bad if your **max_alive_initial_pmcs** or **max_total_bots** settings are high
* Noticeable stuttering for (possibly) several seconds when the initial PMC wave spawns if your **max_alive_initial_pmcs** setting is high

**---------- Roadmap (Expect Similar Accuracy to EFT's) ----------**

* **0.2.7** (ETA: 10/15)
    * **First version posted on SPT-AKI website**
    * Add documentation for config options
    * Add comments to code so I know WTF I'm looking at next year
* **0.3.0** (ETA: Early November)
    * New standard quests for Streets expansion areas
    * Prevent bots from sprinting in more areas
    * Rework quest data structures and logic layer to allow additional actions. Initially planned:
        * Patrol target area for a certain time
        * Wait at specific location for a certain time (mimicing planting items)
    * Implement quest-objective dependencies so certain objectives must be completed immediately before the next one (i.e. go to a specfic location and only then "plant" an item)
    * Another quest-selection algorithm overhaul to replace the "priority" system with a "desirability" score for each quest
    * Add configuration options to overwrite default settings for EFT-based quests and their objectives
* **0.3.1** (ETA: Late November)
    * Add new quest-objective actions: unlocking doors and pulling levers
    * Add new quest type: hidden-stash running
    * Add new quest type: boss hunter
    * Add optional quest prerequisite to have at least one item in a list (i.e. a sniper rifle for sniping areas or an encoded DSP for Lighthouse)
* **0.3.2** (ETA: Mid December)
    * Improve bot-spawn scheduling with initial PMC spawns to prevent them from getting "stuck in the queue" and not spawning until most of the Scavs die
    * Improve PMC senses to dissuade them from going to areas where many bots have died. Might require interaction with SAIN; TBD.
    * Initial PMC group spawns
* **Backlog**
    * Invoke SAIN's logic for having bots extract from the map
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
    * **maxTimeOnQuest**: The maximum time (in seconds) that a bot is allowed to continue doing the quest after it completes at least one of its objectives. This is intended to add more variety to bot questing instead of having them stay in one area for a long period of time. By default, this is 300 seconds.
    * **canRunBetweenObjectives**: Boolean indicating if bots are allowed to sprint to the next objective in the quest after it completes at least one objective. This is intended to be used in areas where stealth is more important (typically in buildings). This is **true** by default. 
    * **name**: The name of the quest. This doesn't have to be unique, but it's best to make it unique to avoid confusion when troubleshooting.
    * **objectives**: An array of the objectives in the quest. Bots can complete objectives in any order. 

* **Objectives**: An objective is a collection of at least one step. An objective represents a list of actions that the bot must complete. Currently, objectives only contain a list of positions that the bot needs to reach. In the future, an example objective could contain multiple types of steps such as: 1) Go to a door, 2) Unlock the door, 3) Go inside of the room.

    Quest objectives have the following properties:
    * **repeatable**: Boolean value indicating if the bot can repeat the quest objective later in the raid. This is typically used for quests are are PvP or PvE focused, where a bot might want to check an area again later in the raid for more enemies.
    * **maxBots**: The maximum number of bots that can actively be performing the objective.
    * **minDistanceFromBot**: The objective will only be selected if the bot is at least this many meters away from it.
    * **maxDistanceFromBot**: The objective will only be selected if the bot is no more than this many meters away from it.
    * **maxRunDistance**: If bots get within this radius of the position for the first step in the objective, they will no longer be allowed to sprint. This is intended to be used in areas where stealth is more important (typically in buildings). This is **0** by default. 
    * **steps**: An array of the steps in the objective. Bots will complete the steps exactly in the order you specify.

* **Steps**: A step is an individual component of an objective. Currently, the only types of objective steps are going to a specific position.

    Quest objective steps have the following properties:
    * **position**: The position on the map that the bot will try to reach

**Tips and Tricks**
* Objectives should be sparsely placed on the map. Since bots take a break from questing after each objective is completed, they will wander around the area (for an unknown distance) before continuing the quest. If you place objective positions too close to each other, the bot will unnecessarily run back and forth around the area. As a rule of thumb, place objectives at least 20m from each other. 
* If you want a bot to go to several specific positions that are close to each other (i.e. adjacent rooms), use multiple steps in a single objectives instead of using multiple objectives. 
* Bots will use the NavMesh to calculate the more efficient path to their objective. They cannot perform complex actions to reach objective locations, so avoid placing objective steps on top of objects (i.e. inside truck beds) or in areas that are difficult to reach.

**---------- Credits ----------**

* Thanks to Props for sharing the code DONUTS uses to spawn bots. This was the inspiration to create this mod. 
* Thanks to DrakiaXYZ for creating BigBrain and Waypoints and for all of your help with developing this mod. 
* Thanks to everyone on Discord who helped to test the many alpha releases of this mod and provided feedback to make it better. There are too many people to name, but you're all awesome. 
* Of course, thanks to the SPT development team who made this possible in the first place. 