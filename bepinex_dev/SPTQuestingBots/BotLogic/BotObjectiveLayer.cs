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

namespace SPTQuestingBots.BotLogic
{
    internal class BotObjectiveLayer : CustomLayer
    {
        private BotObjectiveManager objectiveManager;
        private BotOwner botOwner;
        private BotMonitor botMonitor;
        private float minTimeBetweenSwitchingObjectives = ConfigController.Config.MinTimeBetweenSwitchingObjectives;
        private double searchTimeAfterCombat = ConfigController.Config.SearchTimeAfterCombat.Min;
        private bool wasSearchingForEnemy = false;
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
        private LogicLayerMonitor lootingLayerMonitor;
        private LogicLayerMonitor extractLayerMonitor;
        private LogicLayerMonitor stationaryWSLayerMonitor;
        //private LogicLayerMonitor patrolFollowerLayerMonitor;

        public BotObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            botOwner = _botOwner;
            botMonitor = new BotMonitor(botOwner);

            objectiveManager = botOwner.GetPlayer.gameObject.AddComponent<BotObjectiveManager>();
            objectiveManager.Init(botOwner);

            lootingLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            lootingLayerMonitor.Init(botOwner, "Looting");

            extractLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            extractLayerMonitor.Init(botOwner, "SAIN ExtractLayer");

            stationaryWSLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            stationaryWSLayerMonitor.Init(botOwner, "StationaryWS");

            //patrolFollowerLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            //patrolFollowerLayerMonitor.Init(botOwner, "PatrolFollower");
        }

        public override string GetName()
        {
            return "PMCObjectiveLayer";
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

            if (botOwner.BotFollower.HaveBoss)
            {
                BotOwner boss = botOwner.BotFollower.BossToFollow?.Player()?.AIData?.BotOwner;

                if (boss != null)
                {
                    string bossName = boss.Profile.Nickname;

                    LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " is a follower for " + bossName + ". Disabling questing.");
                    BotQuestController.RegisterBossFollower(boss, botOwner);
                    objectiveManager.StopQuesting();
                    return false;
                }
            }

            if (!BotQuestController.HaveTriggersBeenFound)
            {
                return false;
            }

            if (stationaryWSLayerMonitor.CanLayerBeUsed && stationaryWSLayerMonitor.IsLayerRequested())
            {
                return false;
            }

            if (shouldCheckForLoot(nextLootCheckDelay))
            {
                return pauseLayer(ConfigController.Config.BotQuestingRequirements.BreakForLooting.MaxTimeToStartLooting);
            }

            if (extractLayerMonitor.CanLayerBeUsed)
            {
                string layerName = botOwner.Brain.ActiveLayerName() ?? "null";
                if (layerName.Contains(extractLayerMonitor.LayerName) || extractLayerMonitor.IsLayerRequested())
                {
                    objectiveManager.StopQuesting();

                    LoggingController.LogWarning("Bot " + botOwner.Profile.Nickname + " wants to extract and will no longer quest.");
                    return false;
                }
            }

            if (botMonitor.ShouldWaitForFollowers())
            {
                //LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is waiting for its followers...");
                return pauseLayer(5);
            }

            if (!botMonitor.IsAbleBodied(wasAbleBodied))
            {
                wasAbleBodied = false;
                return pauseLayer();
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is now able-bodied.");
            }
            wasAbleBodied = true;

            if (botMonitor.ShouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!wasSearchingForEnemy)
                {
                    /*bool hasTarget = botOwner.Memory.GoalTarget.HaveMainTarget();
                    if (hasTarget)
                    {
                        string message = "Bot " + botOwner.Profile.Nickname + " is in combat.";
                        message += " Close danger: " + botOwner.Memory.DangerData.HaveCloseDanger + ".";
                        message += " Last Time Hit: " + botOwner.Memory.LastTimeHit + ".";
                        message += " Enemy Set Time: " + botOwner.Memory.EnemySetTime + ".";
                        message += " Last Enemy Seen Time: " + botOwner.Memory.LastEnemyTimeSeen + ".";
                        message += " Under Fire Time: " + botOwner.Memory.UnderFireTime + ".";
                        LoggingController.LogInfo(message);
                    }*/

                    updateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }
                wasSearchingForEnemy = true;
                return pauseLayer();
            }
            wasSearchingForEnemy = false;

            objectiveManager.CanChangeObjective = objectiveManager.TimeSinceChangingObjective > minTimeBetweenSwitchingObjectives;

            if (!objectiveManager.CanReachObjective && !objectiveManager.CanChangeObjective)
            {
                return pauseLayer();
            }

            if (objectiveManager.CanChangeObjective && (objectiveManager.TimeSpentAtObjective > objectiveManager.MinTimeAtObjective))
            {
                string previousObjective = objectiveManager.ToString();
                if (objectiveManager.TryChangeObjective())
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spent " + objectiveManager.TimeSpentAtObjective + "s at it's final position for " + previousObjective);
                }
            }

            if (!objectiveManager.IsObjectiveReached)
            {
                if (checkIfBotIsStuck())
                {
                    if (!wasStuck)
                    {
                        LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is stuck and will get a new objective.");
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

        private void updateSearchTimeAfterCombat()
        {
            System.Random random = new System.Random();
            searchTimeAfterCombat = random.Next((int)ConfigController.Config.SearchTimeAfterCombat.Min, (int)ConfigController.Config.SearchTimeAfterCombat.Max);
        }

        private bool checkIfBotIsStuck()
        {
            if (!lastBotPosition.HasValue)
            {
                lastBotPosition = botOwner.Position;
            }

            float distanceFromLastUpdate = Vector3.Distance(lastBotPosition.Value, botOwner.Position);
            if (distanceFromLastUpdate > ConfigController.Config.StuckBotDetection.Distance)
            {
                lastBotPosition = botOwner.Position;
                botIsStuckTimer.Restart();
            }

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
            Vector3[] failedBotPath = botOwner.Mover?.CurPath;
            if (!ConfigController.Config.Debug.ShowFailedPaths || (failedBotPath == null))
            {
                return;
            }

            List<Vector3> adjustedPathCorners = new List<Vector3>();
            foreach (Vector3 corner in failedBotPath)
            {
                adjustedPathCorners.Add(new Vector3(corner.x, corner.y + 0.75f, corner.z));
            }

            string pathName = "FailedBotPath_" + botOwner.Id;
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

            if (!lootingLayerMonitor.CanLayerBeUsed)
            {
                return false;
            }

            nextLootCheckDelay = ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingChecks;

            string activeLayerName = botOwner.Brain.ActiveLayerName() ?? "null";
            string activeLogicName = BrainManager.GetActiveLogic(botOwner)?.GetType()?.Name ?? "null";
            bool isSearchingForLoot = activeLayerName.Contains(lootingLayerMonitor.LayerName);
            bool isLooting = activeLogicName.Contains("Looting");
            
            if
            (
                (isLooting || (lootSearchTimer.ElapsedMilliseconds < 1000 * ConfigController.Config.BotQuestingRequirements.BreakForLooting.MaxLootScanTime))
                && (isLooting || isSearchingForLoot || lootingLayerMonitor.CanUseLayer(minTimeBetweenLooting))
            )
            {
                //LoggingController.LogInfo("Layer for bot " + botOwner.Profile.Nickname + ": " + activeLayerName + ". Logic: " + activeLogicName);
                
                if (isLooting)
                {
                    if (!hasFoundLoot)
                    {
                        LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has found loot");
                    }

                    nextLootCheckDelay = ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenLootingEvents;
                    lootSearchTimer.Reset();
                    hasFoundLoot = true;
                }
                else
                {
                    if (!wasLooting)
                    {
                        //LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is searching for loot...");
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
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is done looting (Loot searching time: " + (lootSearchTimer.ElapsedMilliseconds / 1000.0) + ").");
            }

            lootSearchTimer.Reset();
            wasLooting = false;
            hasFoundLoot = false;
            return false;
        }
    }
}
