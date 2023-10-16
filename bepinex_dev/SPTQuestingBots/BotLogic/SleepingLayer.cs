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
                return false;
            }

            // Don't run this method too often or performance will be impacted (ironically)
            if (!canUpdate())
            {
                return useLayer;
            }

            // If the bot is allowed to quest, don't allow it to sleep
            BotObjectiveManager objectiveManager = BotOwner.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
            if ((QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value == false) && (objectiveManager?.IsObjectiveActive == true))
            {
                return false;
            }

            // Ensure you're not dead
            Player you = Singleton<GameWorld>.Instance.MainPlayer;
            if (you == null)
            {
                return false;
            }

            // If the bot is close to you, don't allow it to sleep
            if (Vector3.Distance(BotOwner.Position, you.Position) < QuestingBotsPluginConfig.SleepingMinDistanceToYou.Value)
            {
                useLayer = false;
                return useLayer;
            }

            // Enumerate all other alive bots on the map
            IEnumerable<BotOwner> allOtherBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead)
                .Where(b => b.Id != BotOwner.Id);

            foreach (BotOwner bot in allOtherBots)
            {
                // We only care about other bots that can quest
                objectiveManager = bot.GetPlayer.gameObject.GetComponent<BotObjectiveManager>();
                if (objectiveManager?.IsObjectiveActive != true)
                {
                    continue;
                }

                // If a questing bot is close to this one, don't allow this one to sleep
                if (Vector3.Distance(BotOwner.Position, bot.Position) <= QuestingBotsPluginConfig.SleepingMinDistanceToPMCs.Value)
                {
                    useLayer = false;
                    return useLayer;
                }
            }

            useLayer = true;
            return useLayer;
        }

        public override bool IsCurrentActionEnding()
        {
            return !useLayer;
        }
    }
}
