using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using SPT.Custom.CustomAI;
using UnityEngine;

namespace QuestingBots.BotLogic.HiveMind
{
    public enum BotHiveMindSensorType
    {
        Undefined,
        InCombat,
        IsSuspicious,
        CanQuest,
        CanSprintToObjective,
        WantsToLoot
    }

    public class BotHiveMindMonitor : MonoBehaviourDelayedUpdate
    {
        internal static List<BotOwner> deadBots = new List<BotOwner>();
        internal static Dictionary<BotOwner, BotOwner> botGroupLeaders = new Dictionary<BotOwner, BotOwner>();
        internal static Dictionary<BotOwner, List<BotOwner>> botGroupFollowers = new Dictionary<BotOwner, List<BotOwner>>();

        private static Dictionary<BotHiveMindSensorType, BotHiveMindAbstractSensor> sensors = new Dictionary<BotHiveMindSensorType, BotHiveMindAbstractSensor>();

        public BotHiveMindMonitor()
        {
            UpdateInterval = 50;

            sensors.Add(BotHiveMindSensorType.InCombat, new BotHiveMindIsInCombatSensor());
            sensors.Add(BotHiveMindSensorType.IsSuspicious, new BotHiveMindIsSuspiciousSensor());
            sensors.Add(BotHiveMindSensorType.CanQuest, new BotHiveMindCanQuestSensor());
            sensors.Add(BotHiveMindSensorType.CanSprintToObjective, new BotHiveMindCanSprintToObjectiveSensor());
            sensors.Add(BotHiveMindSensorType.WantsToLoot, new BotHiveMindWantsToLootSensor());
        }

        public static void Clear()
        {
            deadBots.Clear();
            botGroupLeaders.Clear();
            botGroupFollowers.Clear();

            sensors.Clear();
        }

        protected void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            if (Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation == null)
            {
                Clear();
                return;
            }

            updateGroupLeaders();
            updateGroupFollowers();

            foreach (BotHiveMindAbstractSensor sensor in sensors.Values)
            {
                sensor.Update();
            }
        }

        public static void UpdateValueForBot(BotHiveMindSensorType sensorType, BotOwner bot, bool value)
        {
            throwIfSensorNotRegistred(sensorType);
            sensors[sensorType].UpdateForBot(bot, value);
        }

        public static bool GetValueForBot(BotHiveMindSensorType sensorType, BotOwner bot)
        {
            throwIfSensorNotRegistred(sensorType);
            return sensors[sensorType].CheckForBot(bot);
        }

        public static bool GetValueForBossOfBot(BotHiveMindSensorType sensorType, BotOwner bot)
        {
            throwIfSensorNotRegistred(sensorType);
            return sensors[sensorType].CheckForBossOfBot(bot);
        }

        public static bool GetValueForFollowers(BotHiveMindSensorType sensorType, BotOwner bot)
        {
            throwIfSensorNotRegistred(sensorType);
            return sensors[sensorType].CheckForFollowers(bot);
        }

        public static bool GetValueForGroup(BotHiveMindSensorType sensorType, BotOwner bot)
        {
            throwIfSensorNotRegistred(sensorType);
            return sensors[sensorType].CheckForGroup(bot);
        }

        public static DateTime GetLastLootingTimeForBoss(BotOwner bot)
        {
            throwIfSensorNotRegistred(BotHiveMindSensorType.WantsToLoot);
            BotHiveMindWantsToLootSensor? sensor = sensors[BotHiveMindSensorType.WantsToLoot] as BotHiveMindWantsToLootSensor;

            return sensor!.GetLastLootingTimeForBoss(bot);
        }

        public static void RegisterBot(BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Cannot register a null bot", nameof(bot));
            }

            if (!botGroupLeaders.ContainsKey(bot))
            {
                botGroupLeaders.Add(bot, null!);
            }

            if (!botGroupFollowers.ContainsKey(bot))
            {
                botGroupFollowers.Add(bot, new List<BotOwner>());
            }

            foreach (BotHiveMindAbstractSensor sensor in sensors.Values)
            {
                sensor.RegisterBot(bot);
            }
        }

        public static bool IsRegistered(BotOwner bot)
        {
            if (bot == null)
            {
                return false;
            }

            return botGroupLeaders.ContainsKey(bot);
        }

        public static bool HasGroupLeader(BotOwner bot)
        {
            return botGroupLeaders.ContainsKey(bot) && (botGroupLeaders[bot] != null);
        }

        public static bool HasGroupFollowers(BotOwner bot)
        {
            return botGroupFollowers.ContainsKey(bot) && (botGroupFollowers[bot]?.Count > 0);
        }

        public static BotOwner GetGroupLeader(BotOwner bot)
        {
            return botGroupLeaders.ContainsKey(bot) ? botGroupLeaders[bot] : null!;
        }

        public static ReadOnlyCollection<BotOwner> GetGroupFollowers(BotOwner bot)
        {
            return botGroupFollowers.ContainsKey(bot) ? botGroupFollowers[bot].AsReadOnly() : new ReadOnlyCollection<BotOwner>(new BotOwner[0]);
        }

        public static IEnumerable<BotOwner> GetAllGroupMembers(BotOwner bot)
        {
            BotOwner leader = GetGroupLeader(bot) ?? bot;
            yield return leader;

            foreach (BotOwner follower in GetGroupFollowers(leader))
            {
                if (follower.Id == bot.Id)
                {
                    continue;
                }

                yield return follower;
            }
        }

        public static string GetActiveBrainLayerOfGroupLeader(BotOwner bot)
        {
            if (!HasGroupLeader(bot) || botGroupLeaders[bot].IsDead)
            {
                return null!;
            }

            return botGroupLeaders[bot].GetActiveLayerTypeName();
        }

        public static float GetDistanceToGroupLeader(BotOwner bot)
        {
            if (!HasGroupLeader(bot))
            {
                return 0;
            }

            return Vector3.Distance(bot.Position, botGroupLeaders[bot].Position);
        }

        public static Vector3? GetLocationOfGroupLeader(BotOwner bot)
        {
            if (!HasGroupLeader(bot))
            {
                return null;
            }

            return botGroupLeaders[bot].Position;
        }

        public static Vector3? GetLocationOfNearestGroupMember(BotOwner bot)
        {
            Vector3? nearestBotPosition = null;
            float nearestBotDistance = float.MaxValue;

            foreach (BotOwner member in GetAllGroupMembers(bot))
            {
                if (member.Id == bot.Id)
                {
                    continue;
                }

                if (member.IsDead)
                {
                    Singleton<LoggingUtil>.Instance.LogError(bot.GetText() + " is trying to regroup with dead follower: " + member.GetText());
                    continue;
                }

                float distance = Vector3.Distance(bot.Position, member.Position);
                if (distance < nearestBotDistance)
                {
                    nearestBotPosition = member.Position;
                    nearestBotDistance = distance;
                }
            }

            return nearestBotPosition;
        }

        public static void SeparateBotFromGroup(BotOwner bot)
        {
            // Not necessary if the bot is solo
            if (bot.BotsGroup.MembersCount <= 1)
            {
                return;
            }

            Singleton<LoggingUtil>.Instance.LogInfo("Separating " + bot.GetText() + " from its group...");

            // Clear stored information about the bot's boss (if applicable)
            foreach (BotOwner follower in botGroupLeaders.Keys.ToArray())
            {
                if (botGroupLeaders[follower] == bot)
                {
                    botGroupLeaders[follower] = null!;
                }

                if (follower == bot)
                {
                    botGroupLeaders[bot] = null!;
                }
            }

            // Clear stored information about the bot's followers (if applicable)
            foreach (BotOwner leaders in botGroupFollowers.Keys.ToArray())
            {
                if (leaders == bot)
                {
                    botGroupFollowers[leaders].Clear();
                }

                if (botGroupFollowers[leaders].Contains(bot))
                {
                    botGroupFollowers[leaders].Remove(bot);
                }
            }

            // If the bot was spawned by this mod, create a new spawn group for it
            if (Singleton<GameWorld>.Instance.GetComponent<BotGenerationManager>().TryGetBotGroupFromAnyGenerator(bot, out Models.BotSpawnInfo matchingGroupData))
            {
                matchingGroupData.SeparateBotOwner(bot);
            }

            // Check if the bot is the leader of its group
            bool isLeader = false;
            if (bot.BotFollower?.HaveBoss == true)
            {
                bot.BotFollower.BossToFollow.RemoveFollower(bot);
                bot.BotFollower.BossToFollow = null;
            }
            else if (bot.Boss.HaveFollowers() && (bot.BotsGroup.BossGroup != null))
            {
                isLeader = true;
            }

            // If the bot is a leader, instruct its followers to follow a new leader
            bot.Boss.RemoveFollower(bot);
            if (isLeader && (bot.Boss.Followers.Count >= 1))
            {
                bot.BotsGroup.BossGroup = null;

                foreach (BotOwner follower in bot.Boss.Followers)
                {
                    follower.BotFollower.BossToFollow = null;
                }

                // Setting a new leader is only required for groups that have more than 2 bots
                if (bot.Boss.Followers.Count > 1)
                {
                    BotOwner newLeader = bot.Boss.Followers.RandomElement();
                    newLeader.Boss.SetBoss(bot.Boss.Followers.Count);

                    if (bot.BotsGroup.BossGroup == null)
                    {
                        Singleton<LoggingUtil>.Instance.LogError("Could not set BossGroup for " + newLeader.GetText());
                    }
                    else
                    {
                        Singleton<LoggingUtil>.Instance.LogInfo("Selected a new group leader for " + bot.Boss.Followers.Count + " followers: " + bot.BotsGroup.BossGroup.Boss.GetText());
                    }
                }
            }

            // Dissociate the bot from its group
            BotsGroup currentGroup = bot.BotsGroup;

            // Create a new bot group for the bot
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            BotZone closestBotZone = botSpawnerClass.GetClosestZone(bot.Position, out float dist);
            BotsGroup newGroup = BotGroupHelpers.CreateGroup(bot, closestBotZone, 1);
            bot.BotsGroup = newGroup;
            newGroup.Lock();

            currentGroup._members.Remove(bot);

            // Make the bot's old group members friendly
            List<BotOwner> oldGroupMembers = currentGroup.GetAllMembers();
            foreach (BotOwner oldGroupMember in oldGroupMembers)
            {
                newGroup.AddAlly(oldGroupMember.GetPlayer);
            }
        }

        private static void throwIfSensorNotRegistred(BotHiveMindSensorType sensorType)
        {
            if (!sensors.ContainsKey(sensorType))
            {
                throw new InvalidOperationException("Sensor type " + sensorType.ToString() + " has not been registerd.");
            }
        }

        private void updateGroupLeaders()
        {
            foreach (BotOwner bot in botGroupLeaders.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if ((bot == null) || bot.IsDead)
                {
                    continue;
                }

                if (botGroupLeaders[bot] == null)
                {
                    botGroupLeaders[bot] = bot.BotFollower?.BossToFollow?.Player()?.AIData?.BotOwner!;
                }
                if (botGroupLeaders[bot] == null)
                {
                    continue;
                }

                if (deadBots.Contains(botGroupLeaders[bot]))
                {
                    botGroupLeaders[bot] = null!;
                    continue;
                }

                if (botGroupLeaders[bot].IsDead)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Group leader " + botGroupLeaders[bot].GetText() + " is now dead.");

                    if (botGroupFollowers.ContainsKey(botGroupLeaders[bot]))
                    {
                        botGroupFollowers.Remove(botGroupLeaders[bot]);
                    }

                    deadBots.Add(botGroupLeaders[bot]);
                    continue;
                }

                addGroupFollower(botGroupLeaders[bot], bot);
            }
        }

        private void addGroupFollower(BotOwner leader, BotOwner bot)
        {
            if (leader == null)
            {
                throw new ArgumentNullException("Leader argument cannot be null", nameof(leader));
            }

            if (bot == null)
            {
                throw new ArgumentNullException("Bot argument cannot be null", nameof(bot));
            }

            if (!botGroupFollowers.ContainsKey(leader))
            {
                //throw new InvalidOperationException("Group leader " + boss.GetText() + " has not been added to the follower dictionary");
                botGroupFollowers.Add(leader, new List<BotOwner>());
            }

            if (!botGroupFollowers[leader].Contains(bot))
            {
                Singleton<LoggingUtil>.Instance.LogInfo("Bot " + bot.GetText() + " is now a follower for " + leader.GetText());
                botGroupFollowers[leader].Add(bot);

                BotJobAssignmentController.CheckBotJobAssignmentValidity(leader);
            }
        }

        private void updateGroupFollowers()
        {
            foreach (BotOwner leader in botGroupFollowers.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if ((leader == null) || leader.IsDead)
                {
                    if (deadBots.Contains(leader!))
                    {
                        continue;
                    }

                    Singleton<LoggingUtil>.Instance.LogDebug("Group leader " + leader.GetText() + " is now dead.");

                    botGroupFollowers.Remove(leader!);
                    deadBots.Add(leader!);

                    continue;
                }

                foreach (BotOwner follower in botGroupFollowers[leader].ToArray())
                {
                    if (follower == null)
                    {
                        Singleton<LoggingUtil>.Instance.LogWarning("Removing null follower for " + leader.GetText());

                        deadBots.Add(follower!);
                    }

                    if (deadBots.Contains(follower!))
                    {
                        if (botGroupFollowers[leader].Contains(follower!))
                        {
                            botGroupFollowers[leader].Remove(follower!);
                        }

                        continue;
                    }

                    if (follower?.IsDead == true)
                    {
                        Singleton<LoggingUtil>.Instance.LogDebug("Follower " + follower.GetText() + " for " + leader.GetText() + " is now dead.");

                        deadBots.Add(follower);
                    }
                }
            }
        }
    }
}
