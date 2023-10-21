using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Models;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Objective
{
    internal class BotObjectiveLayer : CustomLayer
    {
        private BotObjectiveManager objectiveManager;
        private float minTimeBetweenSwitchingObjectives = ConfigController.Config.MinTimeBetweenSwitchingObjectives;
        private double searchTimeAfterCombat = ConfigController.Config.SearchTimeAfterCombat.Min;
        private bool wasAbleBodied = true;
        private bool wasLooting = false;
        private bool hasFoundLoot = false;
        private bool wasStuck = false;
        private Vector3? lastBotPosition = null;
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private float pauseLayerTime = 0;
        private float nextLootCheckDelay = ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;
        private Stopwatch botIsStuckTimer = Stopwatch.StartNew();
        private Stopwatch lootSearchTimer = new Stopwatch();
        private Stopwatch followersTooFarTimer = new Stopwatch();
        private LogicLayerMonitor lootingLayerMonitor;
        private LogicLayerMonitor extractLayerMonitor;
        private LogicLayerMonitor stationaryWSLayerMonitor;

        public BotObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            objectiveManager = BotOwner.GetPlayer.gameObject.AddComponent<BotObjectiveManager>();
            objectiveManager.Init(BotOwner);

            lootingLayerMonitor = BotOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            lootingLayerMonitor.Init(BotOwner, "Looting");

            extractLayerMonitor = BotOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            extractLayerMonitor.Init(BotOwner, "SAIN ExtractLayer");

            stationaryWSLayerMonitor = BotOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            stationaryWSLayerMonitor.Init(BotOwner, "StationaryWS");
        }

        public override string GetName()
        {
            return "BotObjectiveLayer";
        }

        public override Action GetNextAction()
        {
            if (objectiveManager.IsCloseToObjective())
            {
                return new Action(typeof(HoldAtObjectiveAction), "HoldAtObjective");
            }

            return new Action(typeof(GoToObjectiveAction), "GoToObjective");
        }

        public override bool IsCurrentActionEnding()
        {
            return !objectiveManager.IsObjectiveActive || objectiveManager.IsObjectiveReached;
        }

        public override bool IsActive()
        {
            if (pauseLayerTimer.ElapsedMilliseconds < 1000 * pauseLayerTime)
            {
                return false;
            }

            // Check if somebody disabled questing in the F12 menu
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                return false;
            }

            if (BotOwner.BotState != EBotState.Active)
            {
                return false;
            }

            if (!objectiveManager.IsObjectiveActive)
            {
                return false;
            }

            // Check if the bot has a boss that's still alive
            if (BotHiveMindMonitor.HasBoss(BotOwner))
            {
                return false;
            }

            // Ensure all quests have been loaded and generated
            if (!BotQuestController.HaveTriggersBeenFound)
            {
                return false;
            }

            // Check if the bot wants to use a mounted weapon
            if (stationaryWSLayerMonitor.CanLayerBeUsed && stationaryWSLayerMonitor.IsLayerRequested())
            {
                return false;
            }

            if (shouldCheckForLoot(nextLootCheckDelay))
            {
                return pauseLayer(ConfigController.Config.BotQuestingRequirements.BreakForLooting.MaxTimeToStartLooting);
            }

            // Check if the bot is currently extracting or wants to extract via SAIN
            if (extractLayerMonitor.CanLayerBeUsed)
            {
                string layerName = BotOwner.Brain.ActiveLayerName() ?? "null";
                if (layerName.Contains(extractLayerMonitor.LayerName) || extractLayerMonitor.IsLayerRequested())
                {
                    objectiveManager.StopQuesting();

                    LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " wants to extract and will no longer quest.");
                    return false;
                }
            }

            if (objectiveManager.BotMonitor.ShouldWaitForFollowers())
            {
                followersTooFarTimer.Start();
            }
            else
            {
                followersTooFarTimer.Reset();
            }

            if (followersTooFarTimer.ElapsedMilliseconds > ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.MaxWaitTime * 1000)
            {
                //LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is waiting for its followers...");
                return pauseLayer(5);
            }

            if (!objectiveManager.BotMonitor.IsAbleBodied(wasAbleBodied))
            {
                wasAbleBodied = false;
                return pauseLayer();
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is now able-bodied.");
            }
            wasAbleBodied = true;

            if (objectiveManager.BotMonitor.ShouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!BotHiveMindMonitor.IsInCombat(BotOwner))
                {
                    /*bool hasTarget = BotOwner.Memory.GoalTarget.HaveMainTarget();
                    if (hasTarget)
                    {
                        string message = "Bot " + BotOwner.Profile.Nickname + " is in combat.";
                        message += " Close danger: " + BotOwner.Memory.DangerData.HaveCloseDanger + ".";
                        message += " Last Time Hit: " + BotOwner.Memory.LastTimeHit + ".";
                        message += " Enemy Set Time: " + BotOwner.Memory.EnemySetTime + ".";
                        message += " Last Enemy Seen Time: " + BotOwner.Memory.LastEnemyTimeSeen + ".";
                        message += " Under Fire Time: " + BotOwner.Memory.UnderFireTime + ".";
                        LoggingController.LogInfo(message);
                    }*/

                    searchTimeAfterCombat = objectiveManager.BotMonitor.UpdateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }
                BotHiveMindMonitor.UpdateInCombat(BotOwner, true);
                return pauseLayer();
            }
            BotHiveMindMonitor.UpdateInCombat(BotOwner, false);

            // Check if any of the bot's group members are in combat
            // NOTE: This check MUST be performed after updating this bot's combate state!
            if (BotHiveMindMonitor.IsGroupInCombat(BotOwner))
            {
                return false;
            }

            // Check if the bot is allowed to select a new objective based on the time it last selected one
            objectiveManager.CanChangeObjective = objectiveManager.TimeSinceChangingObjective > minTimeBetweenSwitchingObjectives;

            // Temporarily disable the layer if the bot cannot reach its objective and not enough time has passed after it was selected
            if (!objectiveManager.CanReachObjective && !objectiveManager.CanChangeObjective)
            {
                return pauseLayer();
            }

            // Check if the bot has spent enough time at its objective and enough time has passed since it was selected
            if (objectiveManager.CanChangeObjective && (objectiveManager.TimeSpentAtObjective > objectiveManager.MinTimeAtObjective))
            {
                string previousObjective = objectiveManager.ToString();
                if (objectiveManager.TryChangeObjective())
                {
                    LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " spent " + objectiveManager.TimeSpentAtObjective + "s at it's final position for " + previousObjective);
                }
            }

            // Check if the bot has been stuck too many times. The counter resets whenever the bot successfully completes an objective. 
            if (objectiveManager.StuckCount >= ConfigController.Config.StuckBotDetection.MaxCount)
            {
                LoggingController.LogWarning("Bot " + BotOwner.Profile.Nickname + " was stuck " + objectiveManager.StuckCount + " times and likely is unable to quest.");
                objectiveManager.StopQuesting();
                return false;
            }

            if (!objectiveManager.IsObjectiveReached)
            {
                if (checkIfBotIsStuck())
                {
                    if (!wasStuck)
                    {
                        objectiveManager.StuckCount++;
                        LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is stuck and will get a new objective.");
                    }
                    wasStuck = true;
                }
                else
                {
                    wasStuck = false;
                }

                return true;
            }

            return pauseLayer();
        }

        private bool pauseLayer()
        {
            return pauseLayer(0);
        }

        private bool pauseLayer(float minTime)
        {
            pauseLayerTime = minTime;
            pauseLayerTimer.Restart();
            botIsStuckTimer.Restart();
            return false;
        }

        private bool checkIfBotIsStuck()
        {
            if (!lastBotPosition.HasValue)
            {
                lastBotPosition = BotOwner.Position;
            }

            // Check if the bot has moved enough
            float distanceFromLastUpdate = Vector3.Distance(lastBotPosition.Value, BotOwner.Position);
            if (distanceFromLastUpdate > ConfigController.Config.StuckBotDetection.Distance)
            {
                lastBotPosition = BotOwner.Position;
                botIsStuckTimer.Restart();
            }

            // If the bot hasn't moved enough within a certain time while this layer is active, assume the bot is stuck
            if (botIsStuckTimer.ElapsedMilliseconds > 1000 * ConfigController.Config.StuckBotDetection.Time)
            {
                drawStuckBotPath();

                if (objectiveManager.TryChangeObjective())
                {
                    botIsStuckTimer.Restart();
                }

                return true;
            }

            return false;
        }

        private void drawStuckBotPath()
        {
            Vector3[] failedBotPath = BotOwner.Mover?.CurPath;
            if (!ConfigController.Config.Debug.ShowFailedPaths || (failedBotPath == null))
            {
                return;
            }

            List<Vector3> adjustedPathCorners = new List<Vector3>();
            foreach (Vector3 corner in failedBotPath)
            {
                adjustedPathCorners.Add(new Vector3(corner.x, corner.y + 0.75f, corner.z));
            }

            string pathName = "FailedBotPath_" + BotOwner.Id;
            PathRender.RemovePath(pathName);
            PathVisualizationData failedBotPathRendering = new PathVisualizationData(pathName, adjustedPathCorners.ToArray(), Color.red);
            PathRender.AddOrUpdatePath(failedBotPathRendering);
        }

        private bool shouldCheckForLoot(float minTimeBetweenLooting)
        {
            if (!ConfigController.Config.BotQuestingRequirements.BreakForLooting.Enabled)
            {
                return false;
            }

            // Check if LootingBots is loaded
            if (!lootingLayerMonitor.CanLayerBeUsed)
            {
                return false;
            }

            nextLootCheckDelay = ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

            string activeLayerName = BotOwner.Brain.ActiveLayerName() ?? "null";
            string activeLogicName = BrainManager.GetActiveLogic(BotOwner)?.GetType()?.Name ?? "null";

            // Check if the LootingBots logic layer is active
            bool isSearchingForLoot = activeLayerName.Contains(lootingLayerMonitor.LayerName);

            // Check if LootingBots has instructed the bot to check a lootable container
            bool isLooting = activeLogicName.Contains("Looting");

            // The following logic is used to determine if a bot is allowed to search for loot:
            //      - If LootingBots has instructed the bot to check a lootable container, allow it
            //      - If the bot hasn't serached for loot for a minimum amount of time, allow it
            //      - After the minimum amount of time, the bot will only be allowed to search for a certain amount of time. If it doesn't find any loot
            //        in that time, it will be forced to continue questing
            //      - The minimum amount of time between loot checks depends on whether the bot successfully found loot during the previous check
            if
            (
                (isLooting || (lootSearchTimer.ElapsedMilliseconds < 1000 * ConfigController.Config.BotQuestingRequirements.BreakForLooting.MaxLootScanTime))
                && (isLooting || isSearchingForLoot || lootingLayerMonitor.CanUseLayer(minTimeBetweenLooting))
            )
            {
                //LoggingController.LogInfo("Layer for bot " + BotOwner.Profile.Nickname + ": " + activeLayerName + ". Logic: " + activeLogicName);

                if (isLooting)
                {
                    if (!hasFoundLoot)
                    {
                        LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " has found loot");
                    }

                    nextLootCheckDelay = ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingEvents;
                    lootSearchTimer.Reset();
                    hasFoundLoot = true;
                }
                else
                {
                    if (!wasLooting)
                    {
                        //LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is searching for loot...");
                    }

                    lootSearchTimer.Start();
                }

                if (isSearchingForLoot || isLooting)
                {
                    wasLooting = true;
                }

                lootingLayerMonitor.RestartCanUseTimer();
                return true;
            }

            if (wasLooting || hasFoundLoot)
            {
                lootingLayerMonitor.RestartCanUseTimer();
                //LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is done looting (Loot searching time: " + (lootSearchTimer.ElapsedMilliseconds / 1000.0) + ").");
            }

            lootSearchTimer.Reset();
            wasLooting = false;
            hasFoundLoot = false;
            return false;
        }
    }
}
