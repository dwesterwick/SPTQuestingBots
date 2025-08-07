using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotSpawnInfo
    {
        public BotCreationDataClass Data { get; private set; }
        public BotGenerator BotGenerator { get; private set; }
        public bool HasSpawnStarted { get; private set; } = false;
        public bool IsInitialSpawn { get; private set; } = false;
        public Configuration.MinMaxConfig RaidETRangeToSpawn { get; private set; } = new Configuration.MinMaxConfig(0, double.MaxValue);

        private List<BotOwner> bots = new List<BotOwner>();

        public int GeneratedBotCount => Data?.Profiles?.Count ?? 0;
        public int RemainingBotsToSpawn => Math.Max(0, GeneratedBotCount - bots.Count);
        public bool HaveAllBotsSpawned => bots.Count == GeneratedBotCount;
        public bool AreAllAliveBotsActive => bots.Where(b => !b.IsDead).All(b => b.BotState == EBotState.Active);
        public IReadOnlyCollection<BotOwner> SpawnedBots => bots.AsReadOnly();

        public BotSpawnInfo(BotCreationDataClass data, BotGenerator botGenerator)
        {
            Data = data;
            BotGenerator = botGenerator;
        }

        public BotSpawnInfo(BotCreationDataClass data, BotGenerator botGenerator, Configuration.MinMaxConfig raidETRangeToSpawn) : this(data, botGenerator)
        {
            RaidETRangeToSpawn = raidETRangeToSpawn;
        }

        public void StartSpawn()
        {
            HasSpawnStarted = true;

            float secondsSinceSpawning = RaidHelpers.GetSecondsSinceSpawning();
            if (secondsSinceSpawning < 10)
            {
                IsInitialSpawn = true;
            }
        }

        public IReadOnlyCollection<Profile> GetGeneratedProfiles()
        {
            if (GeneratedBotCount == 0)
            {
                return new Profile[0];
            }

            return Data?.Profiles?.AsReadOnly();
        }

        public bool ContainsProfile(Profile profile)
        {
            if (profile == null)
            {
                return false;
            }

            return Data?.Profiles?.Contains(profile) ?? false;
        }

        public bool ShouldBotBeBoss(BotOwner bot)
        {
            if (GeneratedBotCount <= 1)
            {
                return false;
            }

            if (Data.Profiles[0].Id == bot.Profile.Id)
            {
                return true;
            }

            return false;
        }

        public IEnumerator WaitForFollowersAndSetBoss(BotOwner bot)
        {
            LoggingController.LogDebug("Waiting for all bots in group to activate before making " + bot.GetText() + " the boss...");

            while (!HaveAllBotsSpawned)
            {
                yield return new WaitForSeconds(0.01f);
            }

            while (!AreAllAliveBotsActive)
            {
                yield return new WaitForSeconds(0.01f);
            }

            LoggingController.LogDebug("Waiting for all bots in group to activate before making " + bot.GetText() + " the boss...done.");

            bot.Boss.SetBoss(GeneratedBotCount - 1);
        }

        public void AddBotOwner(BotOwner bot)
        {
            if (HaveAllBotsSpawned)
            {
                throw new InvalidOperationException("All BotOwners have already been assigned to the group");
            }

            bots.Add(bot);
        }

        public void SeparateBotOwner(BotOwner botToSeparate)
        {
            if (!HaveAllBotsSpawned)
            {
                throw new InvalidOperationException("Cannot remove a BotOwner from a group that has not spawned yet");
            }

            if (!bots.Contains(botToSeparate))
            {
                LoggingController.LogError("Cannot separate " + botToSeparate.GetText() + " from group that does not contain it");
                return;
            }

            if (!Data.Profiles.Any(p => p == botToSeparate.Profile))
            {
                LoggingController.LogError("Cannot separate " + botToSeparate.GetText() + " from group that does not contain its profile");
                return;
            }

            // Create a new spawn group for the bot
            BotCreationDataClass newData = BotCreationDataClass.CreateWithoutProfile(botToSeparate.SpawnProfileData);
            newData.AddProfile(botToSeparate.Profile);
            Models.BotSpawnInfo newGroup = new BotSpawnInfo(newData, BotGenerator);
            BotGenerator.AddNewBotGroup(newGroup);
            newGroup.AddBotOwner(botToSeparate);

            // Remove the bot from this spawn group
            Data.RemoveProfile(botToSeparate.Profile);
            bots.Remove(botToSeparate);

            if (botToSeparate.Boss.IamBoss && (bots.Count > 1))
            {
                SetNewBoss(bots.RandomElement());
            }

            // TODO: Should we split the BotsGroup too?
        }

        public void SetNewBoss(BotOwner newBoss)
        {
            if (bots.Count <= 1)
            {
                return;
            }

            if (!bots.Contains(newBoss))
            {
                throw new InvalidOperationException("Cannot make " + newBoss.GetText() + " the boss of a group to which he doesn't belong");
            }

            foreach (BotOwner bot in bots)
            {
                bot.Boss.IamBoss = false;
                bot.BotFollower.BossToFollow = null;
            }

            newBoss.Boss.SetBoss(bots.Count - 1);
        }
    }
}
