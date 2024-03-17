using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal abstract class CustomLayerForQuesting : CustomLayerDelayedUpdate
    {
        protected BotLogic.Objective.BotObjectiveManager objectiveManager { get; private set; } = null;

        private double searchTimeAfterCombat = ConfigController.Config.Questing.BotQuestingRequirements.SearchTimeAfterCombat.Min;
        private double suspiciousTime = ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Min;
        private bool wasAbleBodied = true;
        private float maxSuspiciousTime = 60;
        private Stopwatch totalSuspiciousTimer = new Stopwatch();
        private Stopwatch notSuspiciousTimer = Stopwatch.StartNew();
        private Stopwatch notAbleBodiedTimer = new Stopwatch();

        protected float NotAbleBodiedTime => notAbleBodiedTimer.ElapsedMilliseconds / 1000;

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority, int delayInterval) : base(_botOwner, _priority, delayInterval)
        {
            objectiveManager = _botOwner.GetPlayer.gameObject.GetOrAddComponent<BotLogic.Objective.BotObjectiveManager>();
            objectiveManager.Init(_botOwner);

            updateMaxSuspiciousTime();
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
                notAbleBodiedTimer.Start();
                wasAbleBodied = false;

                return false;
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + BotOwner.GetText() + " is now able-bodied.");
            }

            notAbleBodiedTimer.Reset();
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

        protected bool IsSuspicious()
        {
            bool wasSuspiciousTooLong = totalSuspiciousTimer.ElapsedMilliseconds / 1000 > maxSuspiciousTime;
            //if (wasSuspiciousTooLong && totalSuspiciousTimer.IsRunning)
            //{
            //    LoggingController.LogInfo(BotOwner.GetText() + " has been suspicious for too long");
            //}

            if (!wasSuspiciousTooLong && objectiveManager.BotMonitor.ShouldBeSuspicious(suspiciousTime))
            {
                if (!BotHiveMindMonitor.GetValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner))
                {
                    suspiciousTime = objectiveManager.BotMonitor.UpdateSuspiciousTime();
                    //LoggingController.LogInfo("Bot " + BotOwner.GetText() + " will be suspicious for " + suspiciousTime + " seconds");

                    objectiveManager.BotMonitor.TryPreventBotFromLooting((float)suspiciousTime);
                }

                totalSuspiciousTimer.Start();
                notSuspiciousTimer.Reset();

                BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner, true);
                return true;
            }

            if (notSuspiciousTimer.ElapsedMilliseconds / 1000 > ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.SuspicionCooldownTime)
            {
                //if (wasSuspiciousTooLong)
                //{
                //    LoggingController.LogInfo(BotOwner.GetText() + " is now allowed to be suspicious");
                //}

                totalSuspiciousTimer.Reset();
            }
            else
            {
                totalSuspiciousTimer.Stop();
            }

            notSuspiciousTimer.Start();

            BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner, false);
            return false;
        }

        private void updateMaxSuspiciousTime()
        {
            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

            if (ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime.ContainsKey(locationId))
            {
                maxSuspiciousTime = ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime[locationId];
            }
            else if (ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime.ContainsKey("default"))
            {
                maxSuspiciousTime = ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime["default"];
            }
            else
            {
                LoggingController.LogError("Could not set max suspicious time for " + BotOwner.GetText() + ". Defaulting to 60s.");
            }
        }
    }
}
