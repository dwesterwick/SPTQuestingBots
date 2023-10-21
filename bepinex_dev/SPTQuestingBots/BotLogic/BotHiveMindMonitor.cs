using EFT;
using SPTQuestingBots.BehaviorExtensions;
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

        public BotHiveMindMonitor()
        {
            UpdateInterval = 100;
        }

        public static void Clear()
        {
            botBosses.Clear();
            botFollowers.Clear();
            botIsInCombat.Clear();
            botCanQuest.Clear();
            botCanSprintToObjective.Clear();
        }

        private void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            foreach (BotOwner bot in botBosses.Keys.ToArray())
            {
                if (bot == null)
                {
                    continue;
                }

                if (botBosses[bot] == null)
                {
                    botBosses[bot] = bot?.BotFollower?.BossToFollow?.Player()?.AIData?.BotOwner;

                    if (botBosses[bot] != null)
                    {
                        Controllers.LoggingController.LogInfo("The boss for " + bot.Profile.Nickname + " is " + botBosses[bot].Profile.Nickname);
                    }
                }
                else
                {
                    if (!botBosses[bot].isActiveAndEnabled || botBosses[bot].IsDead)
                    {
                        Controllers.LoggingController.LogInfo("Boss " + botBosses[bot].Profile.Nickname + " is now dead.");

                        botBosses[bot] = null;
                    }
                }
            }

            foreach (BotOwner boss in botFollowers.Keys.ToArray())
            {
                if (boss == null)
                {
                    continue;
                }

                foreach (BotOwner bot in botFollowers[boss].ToArray())
                {
                    if (bot == null)
                    {
                        continue;
                    }

                    if (!bot.isActiveAndEnabled || bot.IsDead)
                    {
                        Controllers.LoggingController.LogInfo("Follower " + botBosses[bot].Profile.Nickname + " is now dead.");

                        botFollowers[boss].Remove(bot);
                    }
                }
            }

            foreach (BotOwner bot in botIsInCombat.Keys.ToArray())
            {
                if (bot == null)
                {
                    botIsInCombat.Remove(bot);
                }

                if (!bot.isActiveAndEnabled || bot.IsDead)
                {
                    botIsInCombat[bot] = false;
                }
            }

            foreach (BotOwner bot in botCanQuest.Keys.ToArray())
            {
                if (bot == null)
                {
                    continue;
                }

                if (!bot.isActiveAndEnabled || bot.IsDead)
                {
                    botCanQuest[bot] = false;
                }

                Objective.BotObjectiveManager objectiveManager = bot?.GetPlayer?.gameObject?.GetComponent<Objective.BotObjectiveManager>();
                if (objectiveManager != null)
                {
                    botCanQuest[bot] = objectiveManager.IsObjectiveActive;
                }
            }

            foreach (BotOwner bot in botCanSprintToObjective.Keys.ToArray())
            {
                if (bot == null)
                {
                    continue;
                }

                if (!bot.isActiveAndEnabled || bot.IsDead)
                {
                    botCanSprintToObjective[bot] = false;
                }

                Objective.BotObjectiveManager objectiveManager = bot?.GetPlayer?.gameObject?.GetComponent<Objective.BotObjectiveManager>();
                if (objectiveManager != null)
                {
                    botCanSprintToObjective[bot] = objectiveManager.CanSprintToObjective();
                }
            }
        }

        public static void RegisterBot(BotOwner bot)
        {
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

        private static void updateDictionaryValue<T>(Dictionary<BotOwner, T> dict, BotOwner bot, T value)
        {
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
    }
}
