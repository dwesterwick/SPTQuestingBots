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

            if (WasInCombat(20f))
            {
                //LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " was in combat");
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

            objective.CanChangeObjective = objective.TimeSinceChangingObjective > minTimeBetweenSwitchingObjectives;

            if (!objective.CanReachObjective && !objective.CanChangeObjective)
            {
                return false;
            }

            if (!objective.IsObjectiveReached)
            {
                return true;
            }

            if (objective.CanChangeObjective && (objective.TimeSpentAtObjective > objective.MinTimeAtObjective))
            {
                LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " has spent " + objective.TimeSpentAtObjective + "s at its objective. Setting a new one...");
                objective.ChangeObjective();
                return true;
            }

            return false;
        }

        public override bool IsCurrentActionEnding()
        {
            return !objective.IsObjectiveActive || objective.IsObjectiveReached;
        }

        private bool WasInCombat(float timeframe)
        {
            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < timeframe;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < timeframe;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < timeframe;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < timeframe;

            return wasInCombat;
        }
    }
}
