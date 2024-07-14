using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Models
{
    public class BotSpawnInfo
    {
        public BotCreationDataClass Data { get; private set; }
        public BotGenerator BotGenerator { get; private set; }
        public Configuration.MinMaxConfig RaidETRangeToSpawn { get; private set; } = new Configuration.MinMaxConfig(0, double.MaxValue);

        private List<BotOwner> bots = new List<BotOwner>();

        public bool HasSpawned => bots.Count == Count;
        public int Count => Data?.Profiles?.Count ?? 0;
        public IReadOnlyCollection<BotOwner> SpawnedBots => bots.AsReadOnly();
        public int RemainingBotsToSpawn => Math.Max(0, Count - bots.Count);

        public BotSpawnInfo(BotCreationDataClass data, BotGenerator botGenerator)
        {
            Data = data;
            BotGenerator = botGenerator;
        }

        public BotSpawnInfo(BotCreationDataClass data, BotGenerator botGenerator, Configuration.MinMaxConfig raidETRangeToSpawn) : this(data, botGenerator)
        {
            RaidETRangeToSpawn = raidETRangeToSpawn;
        }

        public bool ShouldBotBeBoss(BotOwner bot)
        {
            if (Count <= 1)
            {
                return false;
            }

            if (Data.Profiles[0].Id == bot.Profile.Id)
            {
                return true;
            }

            return false;
        }

        public void AddBotOwner(BotOwner bot)
        {
            if (HasSpawned)
            {
                throw new InvalidOperationException("All BotOwners have already been assigned to the group");
            }

            bots.Add(bot);
        }

        public void SeparateBotOwner(BotOwner bot)
        {
            if (!HasSpawned)
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
