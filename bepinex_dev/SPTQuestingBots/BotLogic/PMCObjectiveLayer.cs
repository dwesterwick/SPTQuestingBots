using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using QuestingBots.Controllers;
using QuestingBots.Models;
using SPTQuestingBots.BotLogic;
using UnityEngine;
using static HealthControllerClass;

namespace QuestingBots.BotLogic
{
    internal class PMCObjectiveLayer : CustomLayer
    {
        private PMCObjective objective;
        private BotOwner botOwner;
        private float minTimeBetweenSwitchingObjectives = ConfigController.Config.MinTimeBetweenSwitchingObjectives;
        private double searchTimeAfterCombat = ConfigController.Config.SearchTimeAfterCombat.Min;
        private bool wasSearchingForEnemy = false;
        private bool wasAbleBodied = true;
        private bool wasLooting = false;
        private bool wasExtracting = false;
        private bool wasStuck = false;
        private Vector3? lastBotPosition = null;
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private float pauseLayerTime = 0;
        private Stopwatch botIsStuckTimer = Stopwatch.StartNew();
        private LogicLayerMonitor lootingLayerMonitor;
        private LogicLayerMonitor extractLayerMonitor;

        public PMCObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.AddComponent<PMCObjective>();
            objective.Init(botOwner);

            lootingLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            lootingLayerMonitor.Init(botOwner, "Looting");

            extractLayerMonitor = botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            extractLayerMonitor.Init(botOwner, "SAIN ExtractLayer");
        }

        public override string GetName()
        {
            return "PMCObjectiveLayer";
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(PMCObjectiveAction), "GoToObjective");
        }

        public override bool IsActive()
        {
            if (BotOwner.BotState != EBotState.Active)
            {
                return false;
            }

            if (!objective.IsObjectiveActive)
            {
                return false;
            }

            if (!BotQuestController.HaveTriggersBeenFound)
            {
                return false;
            }

            if (pauseLayerTimer.ElapsedMilliseconds / 1000 < pauseLayerTime)
            {
                return false;
            }

            string layerName = botOwner.Brain.ActiveLayerName()?.ToLower();

            bool isLooting = (layerName != null) && layerName.Contains("looting");
            if (isLooting || lootingLayerMonitor.CanUseLayer(10))
            {
                lootingLayerMonitor.RestartUseTimer();

                bool nearStaticLoot = LocationController.TryGetObjectNearPosition(botOwner.Position, 10, out LootItem lootItem);
                bool nearLootContainer = LocationController.TryGetObjectNearPosition(botOwner.Position, 10, out LootableContainer lootContainer);

                if (isLooting || nearStaticLoot ||  nearLootContainer)
                {
                    if (!wasLooting)
                    {
                        string lootDescription = nearStaticLoot ? lootItem.Item.LocalizedName() : "";
                        lootDescription += (nearStaticLoot && nearLootContainer) ? " and " : "";
                        lootDescription += nearLootContainer ? lootContainer.Id : "";

                        LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " can loot " + lootDescription);
                    }

                    wasLooting = true;
                    return pauseLayer(5);
                }
            }
            if (wasLooting)
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is done looting.");
            }
            wasLooting = false;

            if (!isAbleBodied(wasAbleBodied))
            {
                wasAbleBodied = false;
                return pauseLayer();
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is now able-bodied.");
            }
            wasAbleBodied = true;

            if (shouldSearchForEnemy(searchTimeAfterCombat))
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

            objective.CanChangeObjective = objective.TimeSinceChangingObjective > minTimeBetweenSwitchingObjectives;

            if (!objective.CanReachObjective && !objective.CanChangeObjective)
            {
                return pauseLayer();
            }

            if (objective.CanChangeObjective && (objective.TimeSpentAtObjective > objective.MinTimeAtObjective))
            {
                string previousObjective = objective.ToString();
                if (objective.TryChangeObjective())
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " spent " + objective.TimeSpentAtObjective + "s at it's final position for " + previousObjective);
                }
            }

            if (!objective.IsObjectiveReached)
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

        public override bool IsCurrentActionEnding()
        {
            return !objective.IsObjectiveActive || objective.IsObjectiveReached;
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

        private bool shouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasCloseDanger = botOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger;
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
                Vector3[] failedBotPath = botOwner.Mover?.CurPath;
                if (ConfigController.Config.Debug.ShowFailedPaths && (failedBotPath != null))
                {
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

                if (objective.TryChangeObjective())
                {
                    botIsStuckTimer.Restart();
                }

                return true;
            }

            return false;
        }

        private bool isAbleBodied(bool writeToLog)
        {
            if (botOwner.Medecine.FirstAid.Have2Do)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to heal");
                }
                return false;
            }

            if (100f * botOwner.HealthController.Hydration.Current / botOwner.HealthController.Hydration.Maximum < ConfigController.Config.BotQuestingRequirements.MinHydration)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to drink");
                }
                return false;
            }

            if (100f * botOwner.HealthController.Energy.Current / botOwner.HealthController.Energy.Maximum < ConfigController.Config.BotQuestingRequirements.MinEnergy)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to eat");
                }
                return false;
            }

            if 
            (
                (100f * botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head).Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthHead)
                || (100f * botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest).Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthChest)
                || (100f * botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach).Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthStomach)
                || (100f * botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg).Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthLegs)
                || (100f * botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg).Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthLegs)
            )
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " cannot heal");
                }
                return false;
            }

            if (100f * botOwner.GetPlayer.Physical.Overweight > ConfigController.Config.BotQuestingRequirements.MaxOverweightPercentage)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is overweight");
                }
                return false;
            }

            return true;
        }
    }
}
