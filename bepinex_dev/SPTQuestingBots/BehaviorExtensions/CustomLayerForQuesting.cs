using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal abstract class CustomLayerForQuesting : CustomLayerDelayedUpdate
    {
        protected BotLogic.Objective.BotObjectiveManager objectiveManager { get; private set; } = null;

        private double searchTimeAfterCombat = ConfigController.Config.Questing.SearchTimeAfterCombat.Min;
        private bool wasAbleBodied = true;

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority, int delayInterval) : base(_botOwner, _priority, delayInterval)
        {
            objectiveManager = _botOwner.GetPlayer.gameObject.GetOrAddComponent<BotLogic.Objective.BotObjectiveManager>();
            objectiveManager.Init(_botOwner);
        }

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority) : this(_botOwner, _priority, updateInterval)
        {

        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        protected float getPauseRequestTime()
        {
            float pauseTime = objectiveManager.PauseRequest;
            objectiveManager.PauseRequest = 0;

            return pauseTime;
        }

        protected bool IsAbleBodied()
        {
            if (!objectiveManager.BotMonitor.IsAbleBodied(wasAbleBodied))
            {
                wasAbleBodied = false;
                return false;
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is now able-bodied.");
            }
            wasAbleBodied = true;

            return true;
        }

        protected bool IsInCombat()
        {
            if (objectiveManager.BotMonitor.ShouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!BotHiveMindMonitor.GetValueForBot(BotHiveMindSensorType.InCombat, BotOwner))
                {
                    /*bool hasTarget = BotOwner.Memory.GoalTarget.HaveMainTarget();
                    if (hasTarget)
                    {
                        string message = "Bot " + BotOwner.GetText() + " is in combat.";
                        message += " Close danger: " + BotOwner.Memory.DangerData.HaveCloseDanger + ".";
                        message += " Last Time Hit: " + BotOwner.Memory.LastTimeHit + ".";
                        message += " Enemy Set Time: " + BotOwner.Memory.EnemySetTime + ".";
                        message += " Last Enemy Seen Time: " + BotOwner.Memory.LastEnemyTimeSeen + ".";
                        message += " Under Fire Time: " + BotOwner.Memory.UnderFireTime + ".";
                        LoggingController.LogInfo(message);
                    }*/

                    searchTimeAfterCombat = objectiveManager.BotMonitor.UpdateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }
                BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.InCombat, BotOwner, true);
                return true;
            }
            BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.InCombat, BotOwner, false);

            return false;
        }
    }
}
