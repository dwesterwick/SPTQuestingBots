using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Follow
{
    internal class BotFollowerLayer : CustomLayerDelayedUpdate
    {
        private static int updateInterval = 25;

        private Objective.BotObjectiveManager objectiveManager;
        private double searchTimeAfterCombat = ConfigController.Config.SearchTimeAfterCombat.Min;
        private bool wasAbleBodied = true;

        public BotFollowerLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, updateInterval)
        {
            objectiveManager = BotOwner.GetPlayer.gameObject.GetOrAddComponent<Objective.BotObjectiveManager>();
        }

        public override string GetName()
        {
            return "BotFollowerLayer";
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(FollowBossAction), "FollowBoss");
        }

        public override bool IsCurrentActionEnding()
        {
            return false;
        }

        public override bool IsActive()
        {
            if (!canUpdate() && QuestingBotsPluginConfig.QuestingLogicTimeGatingEnabled.Value)
            {
                return previousState;
            }

            // Check if somebody disabled questing in the F12 menu
            if (!QuestingBotsPluginConfig.QuestingEnabled.Value)
            {
                return updatePreviousState(false);
            }

            if (BotOwner.BotState != EBotState.Active)
            {
                return updatePreviousState(false);
            }

            // Only use this layer if the bot has a boss to follow and the boss can quest
            if (!BotHiveMindMonitor.HasBoss(BotOwner) || !BotHiveMindMonitor.CanBossQuest(BotOwner))
            {
                return updatePreviousState(false);
            }

            // Only enable the layer if the bot is too far from the boss
            float? distanceToBoss = BotHiveMindMonitor.GetDistanceToBoss(BotOwner);
            if (!distanceToBoss.HasValue || (distanceToBoss.Value < ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.Target))
            {
                return updatePreviousState(false);
            }

            // Prevent the bot from following its boss if it needs to heal, etc. 
            if (!objectiveManager.BotMonitor.IsAbleBodied(wasAbleBodied))
            {
                wasAbleBodied = false;
                return updatePreviousState(false);
            }
            if (!wasAbleBodied)
            {
                LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " is now able-bodied.");
            }
            wasAbleBodied = true;

            // Prevent the bot from following its boss if it's in combat
            if (objectiveManager.BotMonitor.ShouldSearchForEnemy(searchTimeAfterCombat))
            {
                if (!BotHiveMindMonitor.IsInCombat(BotOwner))
                {
                    searchTimeAfterCombat = objectiveManager.BotMonitor.UpdateSearchTimeAfterCombat();
                    //LoggingController.LogInfo("Bot " + BotOwner.Profile.Nickname + " will spend " + searchTimeAfterCombat + " seconds searching for enemies after combat ends..");
                }
                BotHiveMindMonitor.UpdateInCombat(BotOwner, true);
                return updatePreviousState(false);
            }
            BotHiveMindMonitor.UpdateInCombat(BotOwner, false);

            // If any group members are in combat, the bot should also be in combat
            if (BotHiveMindMonitor.IsGroupInCombat(BotOwner))
            {
                // WIP. Hopefully not needed with SAIN.
                //BotHiveMindMonitor.AssignTargetEnemyFromGroup(BotOwner);

                return updatePreviousState(false);
            }

            // Check if enough time has elapsed since its boss last looted
            TimeSpan timeSinceBossLastLooted = DateTime.Now - BotHiveMindMonitor.GetLastLootingTimeForBoss(BotOwner);
            bool bossWillAllowLooting = timeSinceBossLastLooted.TotalSeconds > ConfigController.Config.BotQuestingRequirements.BreakForLooting.MinTimeBetweenFollowerLootingChecks;

            // Only allow looting if the bot is already looting or its boss will allow it
            if
            (
                (objectiveManager.BotMonitor.ShouldCheckForLoot(objectiveManager.BotMonitor.NextLootCheckDelay) && bossWillAllowLooting)
                || objectiveManager.BotMonitor.IsLooting()
            )
            {
                BotHiveMindMonitor.UpdateWantsToLoot(BotOwner, true);
                return updatePreviousState(pauseLayer(ConfigController.Config.BotQuestingRequirements.BreakForLooting.MaxTimeToStartLooting));
            }
            BotHiveMindMonitor.UpdateWantsToLoot(BotOwner, false);

            return updatePreviousState(true);
        }
    }
}
