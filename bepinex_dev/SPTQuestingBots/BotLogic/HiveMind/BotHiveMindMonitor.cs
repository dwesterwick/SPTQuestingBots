using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.BotLogic.HiveMind
{
    public enum BotHiveMindSensorType
    {
        Undefined,
        InCombat,
        CanQuest,
        CanSprintToObjective,
        WantsToLoot
    }

    public class BotHiveMindMonitor : MonoBehaviourDelayedUpdate
    {
        internal static Dictionary<BotOwner, BotOwner> botBosses = new Dictionary<BotOwner, BotOwner>();
        internal static Dictionary<BotOwner, List<BotOwner>> botFollowers = new Dictionary<BotOwner, List<BotOwner>>();
        private static Dictionary<BotOwner, bool> botFriendlinessUpdated = new Dictionary<BotOwner, bool>();

        private static Dictionary<BotHiveMindSensorType, BotHiveMindAbstractSensor> sensors = new Dictionary<BotHiveMindSensorType, BotHiveMindAbstractSensor>();

        public BotHiveMindMonitor()
        {
            UpdateInterval = 50;

            sensors.Add(BotHiveMindSensorType.InCombat, new BotHiveMindIsInCombatSensor());
            sensors.Add(BotHiveMindSensorType.CanQuest, new BotHiveMindCanQuestSensor());
            sensors.Add(BotHiveMindSensorType.CanSprintToObjective, new BotHiveMindCanSprintToObjectiveSensor());
            sensors.Add(BotHiveMindSensorType.WantsToLoot, new BotHiveMindWantsToLootSensor());
        }

        public static void Clear()
        {
            botBosses.Clear();
            botFollowers.Clear();
            botFriendlinessUpdated.Clear();

            sensors.Clear();
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
            updateBotGroupFriendliness();

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
            BotHiveMindWantsToLootSensor sensor = sensors[BotHiveMindSensorType.WantsToLoot] as BotHiveMindWantsToLootSensor;

            return sensor.GetLastLootingTimeForBoss(bot);
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

            if (!botFriendlinessUpdated.ContainsKey(bot))
            {
                botFriendlinessUpdated.Add(bot, false);
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

        public static float GetDistanceToBoss(BotOwner bot)
        {
            if (!HasBoss(bot))
            {
                return 0;
            }

            return Vector3.Distance(bot.Position, botBosses[bot].Position);
        }

        public static Vector3 GetLocationOfNearestGroupMember(BotOwner bot)
        {
            IReadOnlyCollection<BotOwner> members = GetAllGroupMembers(bot);
            if (members.Count == 0)
            {
                return bot.Position;
            }

            Dictionary<BotOwner, float> distanceToMember = new Dictionary<BotOwner, float>();
            foreach (BotOwner member in members)
            {
                distanceToMember.Add(member, Vector3.Distance(bot.Position, member.Position));
            }

            BotOwner nearestMember = distanceToMember.OrderBy(x => x.Value).First().Key;

            return nearestMember.Position;
        }

        // NOTE: This currently isn't used but it may be in the future
        public static void AssignTargetEnemyFromGroup(BotOwner bot)
        {
            if (bot.Memory.HaveEnemy || bot.Memory.DangerData.HaveCloseDanger)
            {
                return;
            }

            ReadOnlyCollection<BotOwner> groupMembers = GetAllGroupMembers(bot);
            //Controllers.LoggingController.LogInfo("Group members for " + bot.GetText() + ": " + string.Join(", ", groupMembers.Select(m => m.GetText()));

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

                Controllers.LoggingController.LogInfo(member.GetText() + " informed " + bot.GetText() + " about spotted enemy " + bot.Memory.GoalEnemy.Owner.GetText());

                PlaceForCheck enemyLocation = new PlaceForCheck(member.Memory.GoalEnemy.GetPositionForSearch(), PlaceForCheckType.danger);
                bot.Memory.DangerData.SetTarget(enemyLocation, member.Memory.GoalEnemy.Owner);

                return;
            }
        }

        public static void MakeBotHateEveryoneOutsideOfItsGroup(BotOwner bot)
        {
            IReadOnlyCollection<BotOwner> groupMembers = Controllers.Bots.BotGenerator.GetSpawnGroupMembers(bot);
            MakeBotHateEveryoneOutsideOfItsGroup(bot, groupMembers);
        }

        public static void MakeBotHateEveryoneOutsideOfItsGroup(BotOwner bot, IEnumerable<BotOwner> allegedGroupMembers)
        {
            string[] actualGroupMemberIds = allegedGroupMembers.Select(m => m.Profile.Id).ToArray();
            
            IEnumerable<BotOwner> allPlayersOutsideGroup = Singleton<IBotGame>.Instance.BotsController.Bots.BotOwners
                .Where(p => !actualGroupMemberIds.Contains(p.Profile.Id));

            //Controllers.LoggingController.LogInfo(bot.GetText() + "'s group contains: " + string.Join(",", allegedGroupMembers.Select(m => m.GetText())));

            // TO DO: Is this loop actually needed?
            foreach (BotOwner player in allPlayersOutsideGroup)
            {
                if (player.BotsGroup.Allies.Contains(bot))
                {
                    Controllers.LoggingController.LogInfo(player.GetText() + "'s group was initially friendly with " + bot.GetText() + ". Not anymore..");

                    player.BotsGroup.RemoveAlly(bot);
                    player.BotsGroup.AddEnemy(bot, EBotEnemyCause.initial);
                }

                if (bot.BotsGroup.Allies.Contains(player))
                {
                    Controllers.LoggingController.LogInfo(bot.GetText() + "'s group was initially friendly with " + player.GetText() + ". Not anymore..");

                    bot.BotsGroup.RemoveAlly(player);
                    bot.BotsGroup.AddEnemy(player, EBotEnemyCause.initial);
                }
            }

            // Force PMC's to be hostile toward you
            if (Controllers.Bots.BotRegistrationManager.IsBotAPMC(bot) && !bot.BotsGroup.IsPlayerEnemy(Singleton<GameWorld>.Instance.MainPlayer))
            {
                Controllers.LoggingController.LogInfo(bot.GetText() + " doesn't like you anymore");

                bot.BotsGroup.AddEnemy(Singleton<GameWorld>.Instance.MainPlayer, EBotEnemyCause.initial);
            }

            //Controllers.LoggingController.LogInfo(bot.GetText() + "'s group has the following allies: " + string.Join(",", bot.BotsGroup.Allies.Select(a => a.GetText())));
            //Controllers.LoggingController.LogInfo(bot.GetText() + "'s group has the following enemies: " + string.Join(",", bot.BotsGroup.Enemies.Keys.Select(a => a.GetText())));
        }

        private static void throwIfSensorNotRegistred(BotHiveMindSensorType sensorType)
        {
            if (!sensors.ContainsKey(sensorType))
            {
                throw new InvalidOperationException("Sensor type " + sensorType.ToString() + " has not been registerd.");
            }
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

                if (botBosses[bot].IsDead)
                {
                    Controllers.LoggingController.LogInfo("Boss " + botBosses[bot].GetText() + " is now dead.");

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
                throw new InvalidOperationException("Boss " + boss.GetText() + " has not been added to the follower dictionary");
            }

            if (!botFollowers[boss].Contains(bot))
            {
                Controllers.LoggingController.LogInfo("Bot " + bot.GetText() + " is now a follower for " + boss.GetText());

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
                    if ((follower == null) || follower.IsDead)
                    {
                        Controllers.LoggingController.LogInfo("Follower " + follower.GetText() + " for " + boss.GetText() + " is now dead.");

                        botFollowers[boss].Remove(follower);
                    }
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

                // This only needs to be updated once
                if (botFriendlinessUpdated[bot])
                {
                    continue;
                }

                Objective.BotObjectiveManager objectiveManager = Objective.BotObjectiveManager.GetObjectiveManagerForBot(bot);
                if (objectiveManager == null)
                {
                    continue;
                }

                // Wait for a few seconds after the bot has been initialized so EFT can update group properties
                if (objectiveManager.TimeSinceInitialization < 3)
                {
                    continue;
                }

                // If the bot is a member of a group, wait until it has at least one ally or enemy
                IReadOnlyCollection<BotOwner> groupMembers = Controllers.Bots.BotGenerator.GetSpawnGroupMembers(bot);
                if ((groupMembers.Count > 0) && (bot.BotsGroup.Allies.Count == 0) && (bot.BotsGroup.Enemies.Count == 0))
                {
                    continue;
                }

                MakeBotHateEveryoneOutsideOfItsGroup(bot, groupMembers);
                botFriendlinessUpdated[bot] = true;
            }
        }
    }
}
