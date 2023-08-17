using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using EFT.InventoryLogic;
using QuestingBots.Controllers;
using QuestingBots.Models;
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
        private bool wasStuck = false;
        private Vector3? lastBotPosition = null;
        private Stopwatch botIsStuckTimer = Stopwatch.StartNew();

        public PMCObjectiveLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {
            botOwner = _botOwner;

            objective = botOwner.GetPlayer.gameObject.AddComponent<PMCObjective>();
            objective.Init(botOwner);
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
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has spent " + objective.TimeSpentAtObjective + "s at its objective. Setting a new one...");
                objective.ChangeObjective();
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
            botIsStuckTimer.Restart();
            return false;
        }

        private bool shouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasTarget = botOwner.Memory.GoalTarget.HaveMainTarget();
            bool hasCloseDanger = botOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger; //&& hasTarget;
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
            if (distanceFromLastUpdate > 2f)
            {
                lastBotPosition = botOwner.Position;
                botIsStuckTimer.Restart();
            }

            if (botIsStuckTimer.ElapsedMilliseconds > 1000 * 20f)
            {
                Vector3[] failedBotPath = botOwner.Mover?.CurPath;
                if (true && (failedBotPath != null))
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

                objective.ChangeObjective();
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

            if (botOwner.HealthController.Hydration.AtMinimum || botOwner.HealthController.Energy.AtMinimum)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to eat or drink");
                }
                return false;
            }

            if 
            (
                (botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head).Maximum < 0.5)
                || (botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest).Maximum < 0.5)
                || (botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach).Maximum < 0.5)
                || (botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg).Maximum < 0.5)
                || (botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg).Current / botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg).Maximum < 0.5)
            )
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " cannot heal");
                }
                return false;
            }

            if (botOwner.GetPlayer.Physical.Overweight > 1f)
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
