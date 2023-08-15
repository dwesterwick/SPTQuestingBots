using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using QuestingBots.Controllers;
using UnityEngine;

namespace QuestingBots.BotLogic
{
    internal class PMCObjectiveLayer : CustomLayer
    {
        private PMCObjective objective;
        private BotOwner botOwner;
        private float minTimeBetweenSwitchingObjectives = ConfigController.Config.MinTimeBetweenSwitchingObjectives;
        private double searchTimeAfterCombat = ConfigController.Config.SearchTimeAfterCombat.Min;
        private bool wasSearchingForEnemy = false;
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
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is stuck. Changing objective...");
                    objective.ChangeObjective();
                }

                return true;
            }

            return pauseLayer();
        }

        public override bool IsCurrentActionEnding()
        {
            return !objective.IsObjectiveActive || objective.IsObjectiveReached;
        }

        private bool shouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasTarget = botOwner.Memory.GoalTarget.HaveMainTarget();

            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat; //&& hasTarget;
        }

        private void updateSearchTimeAfterCombat()
        {
            System.Random random = new System.Random();
            searchTimeAfterCombat = random.Next((int)ConfigController.Config.SearchTimeAfterCombat.Min, (int)ConfigController.Config.SearchTimeAfterCombat.Max);
        }

        private bool pauseLayer()
        {
            botIsStuckTimer.Restart();
            return false;
        }
    }
}
