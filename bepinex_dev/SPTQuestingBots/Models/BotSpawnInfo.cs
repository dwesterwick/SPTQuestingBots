using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotSpawnInfo
    {
        public BotCreationDataClass Data { get; private set; }
        public BotGenerator BotGenerator { get; private set; }
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

        public IReadOnlyCollection<Profile> GetGeneratedProfiles()
        {
            if (GeneratedBotCount == 0)
            {
                return new Profile[0];
            }

            return Data?.Profiles?.AsReadOnly();
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
            LoggingController.LogInfo("Waiting for all bots in group to activate before making " + bot.GetText() + " the boss...");

            while (!HaveAllBotsSpawned)
            {
                yield return new WaitForSeconds(0.01f);
            }

            while (!AreAllAliveBotsActive)
            {
                yield return new WaitForSeconds(0.01f);
            }

            LoggingController.LogInfo("Waiting for all bots in group to activate before making " + bot.GetText() + " the boss...done.");

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

        public void SeparateBotOwner(BotOwner bot)
        {
            if (!HaveAllBotsSpawned)
            {
                throw new InvalidOperationException("Cannot remove a BotOwner from a group that has not spawned yet");
            }

            if (!bots.Contains(bot))
            {
                LoggingController.LogError("Cannot separate " + bot.GetText() + " from group that does not contain it");
                return;
            }

            if (!Data.Profiles.Any(p => p == bot.Profile))
            {
                LoggingController.LogError("Cannot separate " + bot.GetText() + " from group that does not contain its profile");
                return;
            }

            // Create a new spawn group for the bot
            BotCreationDataClass newData = BotCreationDataClass.CreateWithoutProfile(bot.SpawnProfileData);
            newData.AddProfile(bot.Profile);
            Models.BotSpawnInfo newGroup = new BotSpawnInfo(newData, BotGenerator);
            BotGenerator.AddNewBotGroup(newGroup);
            newGroup.AddBotOwner(bot);

            // Remove the bot from this spawn group
            Data.RemoveProfile(bot.Profile);
            bots.Remove(bot);
        }
    }
}
