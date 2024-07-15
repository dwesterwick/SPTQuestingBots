using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.BotLogic.HiveMind;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class BotFollowerRegroupLayer : CustomLayerForQuesting
    {
        private double maxDistanceFromBoss = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat.Min;

        public BotFollowerRegroupLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 250)
        {

        }

        public override string GetName()
        {
            return "BotFollowerRegroupLayer";
        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        public override bool IsActive()
        {
            if (!canUpdate())
            {
                return previousState;
            }

            // Check if somebody disabled questing in the F12 menu
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.None;
                return updatePreviousState(false);
            }

            if ((BotOwner.BotState != EBotState.Active) || BotOwner.IsDead)
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.IsDead;
                return updatePreviousState(false);
            }

            // Only use this layer if the bot has a boss to follow and the boss can quest
            if (!BotHiveMindMonitor.HasBoss(BotOwner) || !BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.CanQuest, BotOwner))
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.None;
                return updatePreviousState(false);
            }

            // Only use this layer if the bot's boss needs support
            if (!doesBossNeedHelp())
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.None;
                return updatePreviousState(false);
            }

            float pauseRequestTime = getPauseRequestTime();
            if (pauseRequestTime > 0)
            {
                LoggingController.LogInfo("Pausing layer for " + pauseRequestTime + "s...");

                return pauseLayer(pauseRequestTime);
            }

            // Prioritize the bot's safety first
            if (IsInCombat())
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.InCombat;
                return pauseLayer();
            }

            // Prioritize the bot's safety first
            if (IsSuspicious())
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.Suspicious;
                return pauseLayer();
            }

            // Prioritize the bot's safety first
            if (MustHeal())
            {
                if (MustHealTime > ConfigController.Config.Questing.StuckBotDetection.MaxNotAbleBodiedTime)
                {
                    LoggingController.LogWarning("Waited " + MustHealTime + "s for " + BotOwner.GetText() + " to heal");
                    BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);
                }

                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.MustHeal;
                return updatePreviousState(false);
            }

            // Check if the bot has been stuck too many times
            if (objectiveManager.StuckCount >= ConfigController.Config.Questing.StuckBotDetection.MaxCount)
            {
                LoggingController.LogWarning("Bot " + BotOwner.GetText() + " was stuck " + objectiveManager.StuckCount + " times and likely is unable to regroup with its boss.");
                objectiveManager.StopQuesting();
                BotOwner.Mover.Stop();
                BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);

                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.IsStuck;
                return updatePreviousState(false);
            }

            // If the layer is active, run to the boss. Otherwise, allow a little more space
            if (previousState)
            {
                maxDistanceFromBoss = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat.Min;
            }
            else
            {
                maxDistanceFromBoss = ConfigController.Config.Questing.BotQuestingRequirements.MaxFollowerDistance.TargetRangeCombat.Max;
            }

            // Only enable the layer if the bot is too far from the boss
            float? distanceToBoss = BotHiveMindMonitor.GetDistanceToBoss(BotOwner);
            if (!distanceToBoss.HasValue || (distanceToBoss.Value < maxDistanceFromBoss))
            {
                objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.Proximity;
                return updatePreviousState(false);
            }

            objectiveManager.NotRegroupingReason = Objective.NotQuestingReason.None;
            setNextAction(BotActionType.FollowerRegroup, "RegroupWithBoss");
            return updatePreviousState(true);
        }

        private bool doesBossNeedHelp()
        {
            if (BotHiveMindMonitor.GetActiveBrainLayerOfBoss(BotOwner)?.Contains("SAIN") == true)
            {
                return true;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return true;
            }

            if (BotHiveMindMonitor.GetValueForGroup(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return true;
            }

            /*if (BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.InCombat, BotOwner))
            {
                return true;
            }

            if (BotHiveMindMonitor.GetValueForBossOfBot(BotHiveMindSensorType.IsSuspicious, BotOwner))
            {
                return true;
            }*/

            return false;
        }
    }
}
