using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.Models
{
    public class BotSpawnInfo
    {
        public GClass513 Data { get; private set; }
        public Configuration.MinMaxConfig RaidETRangeToSpawn { get; private set; } = new Configuration.MinMaxConfig(0, double.MaxValue);

        private List<BotOwner> bots = new List<BotOwner>();

        public bool HasSpawned => bots.Count == Count;
        public int Count => Data?.Profiles?.Count ?? 0;
        public IReadOnlyCollection<BotOwner> SpawnedBots => bots.AsReadOnly();
        public int RemainingBotsToSpawn => Math.Max(0, Count - bots.Count);

        public BotSpawnInfo(GClass513 data)
        {
            Data = data;
        }

        public BotSpawnInfo(GClass513 data, Configuration.MinMaxConfig raidETRangeToSpawn) : this(data)
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
    }
}
