using Comfort.Common;
using EFT;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Sleep
{
    internal class SleepingLayer : BehaviorExtensions.CustomLayerDelayedUpdate
    {
        private Components.BotObjectiveManager objectiveManager = null;

        public SleepingLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 250)
        {
            objectiveManager = _botOwner.GetOrAddObjectiveManager();
        }

        public override string GetName()
        {
            return "SleepingLayer";
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
            // Check if AI limiting is enabled in the F12 menu
            if (!QuestingBotsPluginConfig.SleepingEnabled.Value)
            {
                return updatePreviousState(false);
            }

            // Don't run this method too often or performance will be impacted (ironically)
            if (!canUpdate())
            {
                return previousState;
            }

            if ((BotOwner.BotState != EBotState.Active) || BotOwner.IsDead)
            {
                return updatePreviousState(false);
            }

            if (isSleeplessBot())
            {
                return updatePreviousState(false);
            }

            // Determine the distance from human players beyond which bots will be disabled
            int mapSpecificHumanDistance = 1000;
            if (QuestingBotsPluginConfig.TarkovMapIDToEnum.TryGetValue(Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id, out TarkovMaps currentMap))
            {
                mapSpecificHumanDistance = getMapSpecificHumanDistance(currentMap);
            }
            int distanceFromHumans = Math.Min(mapSpecificHumanDistance, QuestingBotsPluginConfig.SleepingMinDistanceToHumansGlobal.Value);

            // Check if the bot is currently allowed to quest
            if ((objectiveManager?.IsQuestingAllowed == true) || (objectiveManager?.IsInitialized == false))
            {
                // Check if bots that can quest are allowed to sleep
                if (!QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value)
                {
                    return updatePreviousState(false);
                }

                // If the bot can quest and is allowed to sleep, ensure it's allowed to sleep on the current map
                if (!QuestingBotsPluginConfig.MapsToAllowSleepingForQuestingBots.Value.HasFlag(currentMap))
                {
                    return updatePreviousState(false);
                }
            }

            // Allow bots to extract so new ones can spawn
            if (!QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value && (objectiveManager?.BotMonitor?.GetMonitor<BotExtractMonitor>()?.IsTryingToExtract == true))
            {
                return updatePreviousState(false);
            }

            // Ensure there are still alive human players on the map
            IEnumerable<Player> allPlayers = Singleton<GameWorld>.Instance.AllAlivePlayersList.Where(p => !p.IsAI);
            if (!allPlayers.Any())
            {
                return updatePreviousState(false);
            }

            // If the bot is close to any of the human players, don't allow it to sleep
            if (allPlayers.Any(p => Vector3.Distance(BotOwner.Position, p.Position) < distanceFromHumans))
            {
                return updatePreviousState(false);
            }

            // Enumerate all alive bots on the map
            IEnumerable<BotOwner> allBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead);

            // Only allow bots to sleep if there are at least a certain number in total on the map
            if (allBots.Count() <= QuestingBotsPluginConfig.MinBotsToEnableSleeping.Value)
            {
                return updatePreviousState(false);
            }

            // Of alive bots, enumerate all besides this one that are active
            IEnumerable<BotOwner> allOtherBots = allBots
                .Where(b => b.gameObject.activeSelf)
                .Where(b => b.Id != BotOwner.Id);

            foreach (BotOwner bot in allOtherBots)
            {
                // We only care about other bots that can quest
                Components.BotObjectiveManager otherBotObjectiveManager = bot.GetObjectiveManager();
                if (otherBotObjectiveManager?.IsQuestingAllowed != true)
                {
                    continue;
                }

                // Ignore bots that are in the same group
                List<BotOwner> groupMemberList = SPT.Custom.CustomAI.AiHelpers.GetAllMembers(bot.BotsGroup);
                if (groupMemberList.Contains(BotOwner))
                {
                    continue;
                }

                // If a questing bot is close to this one, don't allow this one to sleep
                if (Vector3.Distance(BotOwner.Position, bot.Position) <= QuestingBotsPluginConfig.SleepingMinDistanceToQuestingBots.Value)
                {
                    return updatePreviousState(false);
                }
            }

            setNextAction(BehaviorExtensions.BotActionType.Sleep, "Sleep");
            return updatePreviousState(true);
        }

        private bool isSleeplessBot()
        {
            if (!QuestingBotsPluginConfig.ExceptionFlagForWildSpawnType.ContainsKey(BotOwner.Profile.Info.Settings.Role))
            {
                return false;
            }

            BotTypeException botTypeException = QuestingBotsPluginConfig.ExceptionFlagForWildSpawnType[BotOwner.Profile.Info.Settings.Role];
            BotTypeException shouldBeSleepless = botTypeException & QuestingBotsPluginConfig.SleeplessBotTypes.Value;

            return shouldBeSleepless > 0;
        }

        private int getMapSpecificHumanDistance(TarkovMaps map)
        {
            switch (map)
            {
                case TarkovMaps.Customs: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansCustoms.Value;
                case TarkovMaps.Factory: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansFactory.Value; 
                case TarkovMaps.Interchange: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansInterchange.Value;
                case TarkovMaps.Labs: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansLabs.Value;
                case TarkovMaps.Lighthouse: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansLighthouse.Value;
                case TarkovMaps.Reserve: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansReserve.Value;
                case TarkovMaps.Shoreline: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansShoreline.Value;
                case TarkovMaps.Streets: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansStreets.Value;
                case TarkovMaps.Woods: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansWoods.Value;
                case TarkovMaps.GroundZero: return QuestingBotsPluginConfig.SleepingMinDistanceToHumansGroundZero.Value;
            }

            return int.MaxValue;
        }
    }
}
