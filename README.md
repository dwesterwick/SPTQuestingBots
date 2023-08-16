**Please remember this is currently in ALPHA testing! Bugs are likely!**

**Requires:**
* BigBrain
* Waypoints

**Tested to be compatible with:**
* SAIN
* Late to the Party (only if **adjust_bot_spawn_chances.enabled=false**)

**Bot Questing Overview:**
* All PMC bots will automatically select random quests, regardless of their spawn time
* There are currently three types of quests available:
    * (50% Chance) Zones and item locations for actual EFT quests (these are dynamically generated using the quest-data from the server, so it will also work with mods that add or change quests)
    * (30% Chance) Going to random spawn points on the map
    * (50% Chance) Rushing your spawn point if it's within 75m and within the first 30s of the raid (configurable)
* After reaching an objective or giving up, the bot will wait a configurable amount of time (currently 10s) before going to its next objective
* If a bot sees and enemy or was recently engaged in combat of any type, it will wait a configurable random amount of time (currently 10-30s) before resuming its quest

**Known Issues for Bot Questing:**
* Bots can't tell if a locked door is blocking their path and will give up instead of unlocking it
* Bots tend to get trapped in certain spawn areas, especially in Factory

**Planned Improvements for Bot Questing:**
* Custom quests (defined via JSON files)
* Specifying the order in which bots should go to quest objectives
* Adding an objective type for waiting a specified amount of time while patrolling the last objective area (for quests like Capturing Outposts)
* Adding a quest for hidden-stash-running with dynamically-generated objectives using the positions of all hidden stashes on the map
* Being able to invoke SAIN's logic for having bots extract from the map
* Adding min/max player levels for quests, so bots will only perform them if their level is reasonable
* Optionally overriding the default priority for EFT quests to make bots more or less likely to select them

**Initial PMC Spawn Overview:**
* PMC's will spawn immediately when the countdown timer at the beginning of the raid reaches 0 (Thanks to Props for sharing code from DONUTS!).
* The number of initial PMC's is a random number between the min and max player count for the map (other mods can change these values)
* PMC's will spawn in actual EFT player spawn points using an algorithm to spread bots out across the map as much as possible
* The PMC difficulty is set by your raid settings in EFT

**Known Issues for Initial PMC Spawns:**
* Rogue and Raider spawning doesn't work correctly
* In maps with a high number of max players (namely Streets), Scavs don't always spawn when the game generates them
* In maps with a high number of max players (namely Streets), performance can be pretty bad
* Noticeable stuttering for the first few seconds of the raid
* Only solo PMC's spawn

**Planned Improvements for Initial PMC Spawns:**
* Adding a config setting for capping the initial PMC spawn count (for people with lower-end PC's)
* Maintain a min/max PMC count vs. raid ET in case the initial PMC spawns are below the max player count for the map
* Add random PMC group spawns