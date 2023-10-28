using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic
{
    public class BotHiveMindMonitor : MonoBehaviourDelayedUpdate
    {
        private static Dictionary<BotOwner, BotOwner> botBosses = new Dictionary<BotOwner, BotOwner>();
        private static Dictionary<BotOwner, List<BotOwner>> botFollowers = new Dictionary<BotOwner, List<BotOwner>>();
        private static Dictionary<BotOwner, bool> botIsInCombat = new Dictionary<BotOwner, bool>();
        private static Dictionary<BotOwner, bool> botCanQuest = new Dictionary<BotOwner, bool>();
        private static Dictionary<BotOwner, bool> botCanSprintToObjective = new Dictionary<BotOwner, bool>();
        private static Dictionary<BotOwner, bool> botWantsToLoot = new Dictionary<BotOwner, bool>();
        private static Dictionary<BotOwner, DateTime> botLastLootingTime = new Dictionary<BotOwner, DateTime>();
        private static Dictionary<BotOwner, bool> botFriendlinessUpdated = new Dictionary<BotOwner, bool>();

        public BotHiveMindMonitor()
        {
            UpdateInterval = 50;
        }

        public static void Clear()
        {
            botBosses.Clear();
            botFollowers.Clear();
            botIsInCombat.Clear();
            botCanQuest.Clear();
            botCanSprintToObjective.Clear();
            botWantsToLoot.Clear();
            botLastLootingTime.Clear();
            botFriendlinessUpdated.Clear();
        }

        private void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            if (LocationController.CurrentLocation == null)
            {
                Clear();
                return;
            }

            updateBosses();
            updateBossFollowers();
            updateBotsInCombat();
            updateIfBotsCanQuest();
            updateIfBotsCanSprintToTheirObjective();
            updateBotsWantToLoot();
            updateBotGroupFriendliness();
        }

        public static void RegisterBot(BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Cannot register a null bot", nameof(bot));
            }

            if (!botBosses.ContainsKey(bot))
            {
                botBosses.Add(bot, null);
            }

            if (!botFollowers.ContainsKey(bot))
            {
                botFollowers.Add(bot, new List<BotOwner>());
            }

            if (!botIsInCombat.ContainsKey(bot))
            {
                botIsInCombat.Add(bot, false);
            }

            if (!botCanQuest.ContainsKey(bot))
            {
                botCanQuest.Add(bot, false);
            }

            if (!botCanSprintToObjective.ContainsKey(bot))
            {
                botCanSprintToObjective.Add(bot, false);
            }

            if (!botWantsToLoot.ContainsKey(bot))
            {
                botWantsToLoot.Add(bot, false);
            }

            if (!botLastLootingTime.ContainsKey(bot))
            {
                botLastLootingTime.Add(bot, DateTime.MinValue);
            }

            if (!botFriendlinessUpdated.ContainsKey(bot))
            {
                botFriendlinessUpdated.Add(bot, false);
            }
        }

        public static bool IsRegistered(BotOwner bot)
        {
            if (bot == null)
            {
                return false;
            }

            return botBosses.ContainsKey(bot);
        }

        public static bool HasBoss(BotOwner bot)
        {
            return botBosses.ContainsKey(bot) && (botBosses[bot] != null);
        }

        public static bool HasFollowers(BotOwner bot)
        {
            return botFollowers.ContainsKey(bot) && (botFollowers[bot]?.Count > 0);
        }

        public static BotOwner GetBoss(BotOwner bot)
        {
            return botBosses.ContainsKey(bot) ? botBosses[bot] : null;
        }

        public static ReadOnlyCollection<BotOwner> GetFollowers(BotOwner bot)
        {
            return botFollowers.ContainsKey(bot) ? new ReadOnlyCollection<BotOwner>(botFollowers[bot]) : new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
        }

        public static ReadOnlyCollection<BotOwner> GetAllGroupMembers(BotOwner bot)
        {
            BotOwner boss = GetBoss(bot) ?? bot;

            BotOwner[] allGroupMembers = GetFollowers(boss)
                .AddItem(boss)
                .Where(b => b.Id != bot.Id)
                .ToArray();

            return new ReadOnlyCollection<BotOwner>(allGroupMembers);
        }

        public static float? GetDistanceToBoss(BotOwner bot)
        {
            if (!HasBoss(bot))
            {
                return null;
            }

            return Vector3.Distance(bot.Position, botBosses[bot].Position);
        }

        public static void UpdateInCombat(BotOwner bot, bool inCombat)
        {
            updateDictionaryValue(botIsInCombat, bot, inCombat);
        }

        public static bool IsInCombat(BotOwner bot)
        {
            return botIsInCombat.ContainsKey(bot) && botIsInCombat[bot];
        }

        public static bool IsBossInCombat(BotOwner bot)
        {
            return checkBotState(botIsInCombat, GetBoss(bot)) ?? false;
        }

        public static bool AreFollowersInCombat(BotOwner bot)
        {
            return checkStateForAnyFollowers(botIsInCombat, bot);
        }

        public static bool IsGroupInCombat(BotOwner bot)
        {
            return checkStateForAnyGroupMembers(botIsInCombat, bot);
        }

        public static bool CanQuest(BotOwner bot)
        {
            return botCanQuest.ContainsKey(bot) && botCanQuest[bot];
        }

        public static bool CanBossQuest(BotOwner bot)
        {
            return checkBotState(botCanQuest, GetBoss(bot)) ?? false;
        }

        public static bool CanFollowersQuest(BotOwner bot)
        {
            return checkStateForAnyFollowers(botCanQuest, bot);
        }

        public static bool CanGroupQuest(BotOwner bot)
        {
            return checkStateForAnyGroupMembers(botCanQuest, bot);
        }

        public static bool CanSprintToObjective(BotOwner bot)
        {
            return botCanSprintToObjective.ContainsKey(bot) && botCanSprintToObjective[bot];
        }

        public static bool CanBossSprintToObjective(BotOwner bot)
        {
            return checkBotState(botCanSprintToObjective, GetBoss(bot)) ?? true;
        }

        public static bool CanFollowersSprintToObjective(BotOwner bot)
        {
            return checkStateForAnyFollowers(botCanSprintToObjective, bot);
        }

        public static bool CanGroupSprintToObjective(BotOwner bot)
        {
            return checkStateForAnyGroupMembers(botCanSprintToObjective, bot);
        }

        public static void UpdateWantsToLoot(BotOwner bot, bool wantsToLoot)
        {
            updateDictionaryValue(botWantsToLoot, bot, wantsToLoot);

            if (wantsToLoot && (bot != null))
            {
                botLastLootingTime[bot] = DateTime.Now;
            }
        }

        public static bool WantsToLoot(BotOwner bot)
        {
            return botWantsToLoot.ContainsKey(bot) && botWantsToLoot[bot];
        }

        public static bool DoesBossWantToLoot(BotOwner bot)
        {
            return checkBotState(botWantsToLoot, GetBoss(bot)) ?? false;
        }

        public static bool DoFollowersWantToLoot(BotOwner bot)
        {
            return checkStateForAnyFollowers(botWantsToLoot, bot);
        }

        public static bool DoesGroupWantToLoot(BotOwner bot)
        {
            return checkStateForAnyGroupMembers(botWantsToLoot, bot);
        }

        public static DateTime GetLastLootingTimeForBoss(BotOwner bot)
        {
            if ((bot == null) || !botBosses.ContainsKey(bot) || (botBosses[bot] == null))
            {
                return DateTime.MinValue;
            }

            return botLastLootingTime[bot];
        }

        public static void AssignTargetEnemyFromGroup(BotOwner bot)
        {
            if (bot.Memory.HaveEnemy || bot.Memory.DangerData.HaveCloseDanger)
            {
                return;
            }

            ReadOnlyCollection<BotOwner> groupMembers = GetAllGroupMembers(bot);
            //Controllers.LoggingController.LogInfo("Group members for " + bot.Profile.Nickname + ": " + string.Join(", ", groupMembers.Select(m => m.Profile.Nickname));

            foreach (BotOwner member in groupMembers)
            {
                if (!member.isActiveAndEnabled || member.IsDead)
                {
                    continue;
                }

                if (!member.Memory.HaveEnemy)
                {
                    continue;
                }

                Controllers.LoggingController.LogInfo(member.Profile.Nickname + " informed " + bot.Profile.Nickname + " about spotted enemy " + bot.Memory.GoalEnemy.Owner.Profile.Nickname);

                PlaceForCheck enemyLocation = new PlaceForCheck(member.Memory.GoalEnemy.GetPositionForSearch(), PlaceForCheckType.danger);
                bot.Memory.DangerData.SetTarget(enemyLocation, member.Memory.GoalEnemy.Owner);

                return;
            }
        }

        public static void MakeBotHateEveryoneOutsideOfItsGroup(BotOwner bot)
        {
            IReadOnlyCollection<BotOwner> groupMembers = BotGenerator.GetSpawnGroupMembers(bot);
            MakeBotHateEveryoneOutsideOfItsGroup(bot, groupMembers);
        }

        public static void MakeBotHateEveryoneOutsideOfItsGroup(BotOwner bot, IEnumerable<BotOwner> allegedGroupMembers)
        {
            string[] actualGroupMemberIds = allegedGroupMembers.Select(m => m.Profile.Id).ToArray();
            
            IEnumerable<BotOwner> allPlayersOutsideGroup = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(p => !actualGroupMemberIds.Contains(p.Profile.Id));

            //Controllers.LoggingController.LogInfo(bot.Profile.Nickname + "'s group contains: " + string.Join(",", allegedGroupMembers.Select(m => m.Profile.Nickname)));

            foreach (BotOwner player in allPlayersOutsideGroup)
            {
                if (player.BotsGroup.Allies.Contains(bot))
                {
                    Controllers.LoggingController.LogInfo(player.Profile.Nickname + "'s group was initially friendly with " + bot.Profile.Nickname + ". Not anymore..");

                    player.BotsGroup.RemoveAlly(bot);
                    player.BotsGroup.AddEnemy(bot, EBotEnemyCause.initial);
                }

                if (bot.BotsGroup.Allies.Contains(player))
                {
                    Controllers.LoggingController.LogInfo(bot.Profile.Nickname + "'s group was initially friendly with " + player.Profile.Nickname + ". Not anymore..");

                    bot.BotsGroup.RemoveAlly(player);
                    bot.BotsGroup.AddEnemy(player, EBotEnemyCause.initial);
                }
            }

            if (BotQuestController.IsBotAPMC(bot) && !bot.BotsGroup.IsPlayerEnemy(Singleton<GameWorld>.Instance.MainPlayer))
            {
                Controllers.LoggingController.LogInfo(bot.Profile.Nickname + " doesn't like you anymore");

                bot.BotsGroup.AddEnemy(Singleton<GameWorld>.Instance.MainPlayer, EBotEnemyCause.initial);
            }

            //Controllers.LoggingController.LogInfo(bot.Profile.Nickname + "'s group has the following allies: " + string.Join(",", bot.BotsGroup.Allies.Select(a => a.Profile.Nickname)));
            //Controllers.LoggingController.LogInfo(bot.Profile.Nickname + "'s group has the following enemies: " + string.Join(",", bot.BotsGroup.Enemies.Keys.Select(a => a.Profile.Nickname)));
        }

        private static void updateDictionaryValue<T>(Dictionary<BotOwner, T> dict, BotOwner bot, T value)
        {
            if (bot == null)
            {
                return;
            }

            if (dict.ContainsKey(bot))
            {
                dict[bot] = value;
            }
            else
            {
                dict.Add(bot, value);
            }
        }

        private static bool? checkBotState(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            if (dict.TryGetValue(bot, out bool value))
            {
                return value;
            }

            return null;
        }

        private static bool checkStateForAnyFollowers(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            if (!botFollowers.ContainsKey(bot))
            {
                return false;
            }

            foreach (BotOwner follower in botFollowers[bot].ToArray())
            {
                if (!dict.TryGetValue(follower, out bool value))
                {
                    continue;
                }

                if (value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool checkStateForAnyGroupMembers(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            if (checkBotState(dict, bot) == true)
            {
                return true;
            }

            foreach (BotOwner boss in botFollowers.Keys.ToArray())
            {
                if (checkStateForAnyFollowers(dict, boss))
                {
                    return true;
                }
            }

            return false;
        }

        private void updateBosses()
        {
            foreach (BotOwner bot in botBosses.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if (botBosses[bot] == null)
                {
                    botBosses[bot] = bot.BotFollower?.BossToFollow?.Player()?.AIData?.BotOwner;

                    if (botBosses[bot] != null)
                    {
                        addBossFollower(botBosses[bot], bot);
                    }

                    continue;
                }

                if (!botBosses[bot].isActiveAndEnabled || botBosses[bot].IsDead)
                {
                    Controllers.LoggingController.LogInfo("Boss " + botBosses[bot].Profile.Nickname + " is now dead.");

                    if (botFollowers.ContainsKey(botBosses[bot]))
                    {
                        botFollowers.Remove(botBosses[bot]);
                    }

                    botBosses[bot] = null;
                }
            }
        }

        private void addBossFollower(BotOwner boss, BotOwner bot)
        {
            if (boss == null)
            {
                throw new ArgumentNullException("Boss argument cannot be null", nameof(boss));
            }

            if (bot == null)
            {
                throw new ArgumentNullException("Bot argument cannot be null", nameof(bot));
            }

            if (!botFollowers.ContainsKey(boss))
            {
                throw new InvalidOperationException("Boss " + boss.Profile.Nickname + " has not been added to the follower dictionary");
            }

            if (!botFollowers[boss].Contains(bot))
            {
                Controllers.LoggingController.LogInfo("Bot " + bot.Profile.Nickname + " is now a follower for " + boss.Profile.Nickname);

                botFollowers[boss].Add(bot);
            }
        }

        private void updateBossFollowers()
        {
            foreach (BotOwner boss in botFollowers.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (boss == null)
                {
                    continue;
                }

                foreach (BotOwner follower in botFollowers[boss].ToArray())
                {
                    if ((follower == null) || !follower.isActiveAndEnabled || follower.IsDead)
                    {
                        Controllers.LoggingController.LogInfo("Follower " + follower.Profile.Nickname + " for " + boss.Profile.Nickname + " is now dead.");

                        botFollowers[boss].Remove(follower);
                    }
                }
            }
        }

        private void updateBotsInCombat()
        {
            foreach (BotOwner bot in botIsInCombat.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if ((bot?.isActiveAndEnabled == false) || (bot?.IsDead == true))
                {
                    botIsInCombat[bot] = false;
                }
            }
        }

        private void updateIfBotsCanQuest()
        {
            foreach (BotOwner bot in botCanQuest.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if ((bot?.isActiveAndEnabled == false) || (bot?.IsDead == true))
                {
                    botCanQuest[bot] = false;
                }

                if (bot?.GetPlayer?.gameObject?.TryGetComponent(out Objective.BotObjectiveManager objectiveManager) == true)
                {
                    botCanQuest[bot] = objectiveManager?.IsObjectiveActive ?? false;
                }
            }
        }

        private void updateIfBotsCanSprintToTheirObjective()
        {
            foreach (BotOwner bot in botCanSprintToObjective.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if ((bot?.isActiveAndEnabled == false) || (bot?.IsDead == true))
                {
                    botCanSprintToObjective[bot] = true;
                }

                if (bot?.GetPlayer?.gameObject?.TryGetComponent(out Objective.BotObjectiveManager objectiveManager) == true)
                {
                    botCanSprintToObjective[bot] = objectiveManager?.CanSprintToObjective() ?? true;
                }
            }
        }

        private void updateBotsWantToLoot()
        {
            foreach (BotOwner bot in botWantsToLoot.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if ((bot?.isActiveAndEnabled == false) || (bot?.IsDead == true))
                {
                    botWantsToLoot[bot] = false;
                }
            }
        }

        private void updateBotGroupFriendliness()
        {
            foreach (BotOwner bot in botFriendlinessUpdated.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if (botFriendlinessUpdated[bot])
                {
                    continue;
                }

                Objective.BotObjectiveManager objectiveManager = null;
                if (bot?.GetPlayer?.gameObject?.TryGetComponent(out objectiveManager) == false)
                {
                    continue;
                }

                double timeSinceInitialized = objectiveManager?.TimeSinceInitialization ?? 0;

                IReadOnlyCollection<BotOwner> groupMembers = BotGenerator.GetSpawnGroupMembers(bot);
                if ((timeSinceInitialized < 3) && (groupMembers.Count > 0) && (bot.BotsGroup.Allies.Count == 0) && (bot.BotsGroup.Enemies.Count == 0))
                {
                    continue;
                }

                MakeBotHateEveryoneOutsideOfItsGroup(bot, groupMembers);
                botFriendlinessUpdated[bot] = true;
            }
        }
    }
}
