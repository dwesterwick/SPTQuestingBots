using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace SPTQuestingBots.BotLogic
{
    internal class SleepingLayer : CustomLayerDelayedUpdate
    {
        private static int updateInterval = 250;

        private bool useLayer = false;
        private BotObjectiveManager objectiveManager = null;

        public SleepingLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, updateInterval)
        {
            
        }

        public override string GetName()
        {
            return "SleepingLayer";
        }

        public override Action GetNextAction()
        {
            return new Action(typeof(SleepingAction), "Sleeping");
        }

        public override bool IsActive()
        {
            // Check if AI limiting is enabled in the F12 menu
            if (!QuestingBotsPluginConfig.SleepingEnabled.Value)
            {
                return updateUseLayer(false);
            }

            // Don't run this method too often or performance will be impacted (ironically)
            if (!canUpdate())
            {
                return useLayer;
            }

            // Check if the bot was ever allowed to quest
            if (objectiveManager == null)
            {
                objectiveManager = BotOwner.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
            }

            // Check if the bot is currently allowed to quest
            if ((objectiveManager?.IsObjectiveActive == true) || (objectiveManager?.IsInitialized == false))
            {
                // Check if bots that can quest are allowed to sleep
                if (!QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value)
                {
                    return updateUseLayer(false);
                }

                // If the bot can quest and is allowed to sleep, ensure it's allowed to sleep on the current map
                if (QuestingBotsPluginConfig.TarkovMapIDToEnum.TryGetValue(Controllers.LocationController.CurrentLocation?.Id, out TarkovMaps map))
                {
                    if (!QuestingBotsPluginConfig.MapsToAllowSleepingForQuestingBots.Value.HasFlag(map))
                    {
                        return updateUseLayer(false);
                    }
                }
            }

            // Ensure you're not dead
            Player you = Singleton<GameWorld>.Instance.MainPlayer;
            if (you == null)
            {
                return updateUseLayer(false);
            }

            // If the bot is close to you, don't allow it to sleep
            if (Vector3.Distance(BotOwner.Position, you.Position) < QuestingBotsPluginConfig.SleepingMinDistanceToYou.Value)
            {
                return updateUseLayer(false);
            }

            // Enumerate all other alive bots on the map
            IEnumerable<BotOwner> allOtherBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead)
                .Where(b => b.Id != BotOwner.Id);

            foreach (BotOwner bot in allOtherBots)
            {
                // We only care about other bots that can quest
                BotObjectiveManager otherBotObjectiveManager = bot.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
                if (otherBotObjectiveManager?.IsObjectiveActive != true)
                {
                    continue;
                }

                // If a questing bot is close to this one, don't allow this one to sleep
                if (Vector3.Distance(BotOwner.Position, bot.Position) <= QuestingBotsPluginConfig.SleepingMinDistanceToPMCs.Value)
                {
                    return updateUseLayer(false);
                }
            }

            return updateUseLayer(true);
        }

        public override bool IsCurrentActionEnding()
        {
            return !useLayer;
        }

        private bool updateUseLayer(bool newValue)
        {
            useLayer = newValue;
            return useLayer;
        }
    }
}
