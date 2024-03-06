using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.Sleep
{
    internal class SleepingLayer : BehaviorExtensions.CustomLayerDelayedUpdate
    {
        private Objective.BotObjectiveManager objectiveManager = null;

        public SleepingLayer(BotOwner _botOwner, int _priority) : base(_botOwner, _priority, 250)
        {
            
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

            // Unless enabled in the F12 menu, do not allow sniper Scavs to sleep
            if (!QuestingBotsPluginConfig.SleepingEnabledForSniperScavs.Value && (BotOwner.Profile.Info.Settings.Role == WildSpawnType.marksman))
            {
                return updatePreviousState(false);
            }

            // Check if the bot was ever allowed to quest
            if (objectiveManager == null)
            {
                objectiveManager = BotOwner.GetPlayer.gameObject.GetComponent<Objective.BotObjectiveManager>();
            }

            // Check if the bot is currently allowed to quest
            if ((objectiveManager?.IsQuestingAllowed == true) || (objectiveManager?.IsInitialized == false))
            {
                // Check if bots that can quest are allowed to sleep
                if (!QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value)
                {
                    return updatePreviousState(false);
                }

                // If the bot can quest and is allowed to sleep, ensure it's allowed to sleep on the current map
                if (QuestingBotsPluginConfig.TarkovMapIDToEnum.TryGetValue(Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id, out TarkovMaps map))
                {
                    if (!QuestingBotsPluginConfig.MapsToAllowSleepingForQuestingBots.Value.HasFlag(map))
                    {
                        return updatePreviousState(false);
                    }
                }
            }

            // Allow bots to extract so new ones can spawn
            if (!QuestingBotsPluginConfig.SleepingEnabledForQuestingBots.Value && (objectiveManager?.BotMonitor?.IsTryingToExtract() == true))
            {
                return updatePreviousState(false);
            }

            // Ensure you're not dead
            Player you = Singleton<GameWorld>.Instance.MainPlayer;
            if (you == null)
            {
                return updatePreviousState(false);
            }

            // If the bot is close to you, don't allow it to sleep
            if (Vector3.Distance(BotOwner.Position, you.Position) < QuestingBotsPluginConfig.SleepingMinDistanceToYou.Value)
            {
                return updatePreviousState(false);
            }

            // Enumerate all other bots on the map that are alive and active
            IEnumerable<BotOwner> allOtherBots = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(b => b.BotState == EBotState.Active)
                .Where(b => !b.IsDead)
                .Where(b => b.gameObject.activeSelf)
                .Where(b => b.Id != BotOwner.Id);

            foreach (BotOwner bot in allOtherBots)
            {
                // We only care about other bots that can quest
                Objective.BotObjectiveManager otherBotObjectiveManager = bot.GetPlayer.gameObject.GetComponent<Objective.BotObjectiveManager>();
                if (otherBotObjectiveManager?.IsQuestingAllowed != true)
                {
                    continue;
                }

                // Get the bot's current group members
                List<BotOwner> groupMemberList = new List<BotOwner>();
                for (int m = 0; m < bot.BotsGroup.MembersCount; m++)
                {
                    groupMemberList.Add(bot.BotsGroup.Member(m));
                }

                // Ignore bots that are in the same group
                if (groupMemberList.Contains(BotOwner))
                {
                    continue;
                }

                // If a questing bot is close to this one, don't allow this one to sleep
                if (Vector3.Distance(BotOwner.Position, bot.Position) <= QuestingBotsPluginConfig.SleepingMinDistanceToPMCs.Value)
                {
                    return updatePreviousState(false);
                }
            }

            setNextAction(BehaviorExtensions.BotActionType.Sleep, "Sleep");
            return updatePreviousState(true);
        }
    }
}
