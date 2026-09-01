using EFT;
using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Components.Spawning
{
    public class BotGenerationManager : MonoBehaviour
    {
        private List<BotGenerator> activeBotGenerators = new List<BotGenerator>();
        private Dictionary<BotOwner, Models.BotSpawnInfo> botSpawnInfoCache = new Dictionary<BotOwner, Models.BotSpawnInfo>();

        public void AddActiveBotGenerator(BotGenerator botGenerator)
        {
            activeBotGenerators.Add(botGenerator);
        }

        public IEnumerable<Models.BotSpawnInfo> GetAllBotGroups()
        {
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                foreach (BotSpawnInfo botSpawnInfo in botGenerator.GetBotGroups())
                {
                    yield return botSpawnInfo;
                }
            }
        }

        public bool IsPositionCloseToAnyGeneratedBots(Vector3 position, float distanceFromPlayers, out float distance)
        {
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (botGenerator == null)
                {
                    continue;
                }

                if (IsPositionCloseToGeneratedBots(position, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public bool AreAnyPositionsCloseToAnyGeneratedBots(IEnumerable<Vector3> positions, float distanceFromPlayers, out float distance)
        {
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (botGenerator == null)
                {
                    continue;
                }

                if (AreAnyPositionsCloseToGeneratedBots(positions, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public IEnumerable<string> GetAllGeneratedBotProfileIDs()
        {
            return GetAllGeneratedBotProfiles().Select(b => b.Id);
        }

        public IEnumerable<Profile> GetAllGeneratedBotProfiles()
        {
            List<Profile> generatedBotProfiles = new List<Profile>();
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (botGenerator == null)
                {
                    continue;
                }

                generatedBotProfiles.AddRange(GetGeneratedBotProfiles());
            }

            return generatedBotProfiles;
        }

        public bool AreAnyPositionsCloseToGeneratedBots(IEnumerable<Vector3> positions, float distanceFromPlayers, out float distance)
        {
            foreach (Vector3 position in positions)
            {
                if (IsPositionCloseToGeneratedBots(position, distanceFromPlayers, out distance))
                {
                    return true;
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public bool IsPositionCloseToGeneratedBots(Vector3 position, float distanceFromPlayers, out float distance)
        {
            foreach (Models.BotSpawnInfo botGroup in GetAllBotGroups())
            {
                IEnumerable<BotOwner> aliveBots = botGroup.SpawnedBots.Where(b => (b != null) && !b.IsDead);
                foreach (BotOwner bot in aliveBots)
                {
                    distance = Vector3.Distance(bot.Position, position);
                    if (distance <= distanceFromPlayers)
                    {
                        return true;
                    }
                }
            }

            distance = float.MaxValue;
            return false;
        }

        public IEnumerable<string> GetGeneratedBotProfileIDs()
        {
            return GetGeneratedBotProfiles().Select(b => b.Id);
        }

        public IEnumerable<Profile> GetGeneratedBotProfiles()
        {
            List<Profile> generatedBotProfiles = new List<Profile>();

            foreach (Models.BotSpawnInfo botGroup in GetAllBotGroups())
            {
                generatedBotProfiles.AddRange(botGroup.Data.Profiles);
            }

            return generatedBotProfiles;
        }

        public bool TryGetBotGroup(BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            matchingGroupData = null!;

            foreach (Models.BotSpawnInfo info in GetAllBotGroups())
            {
                foreach (Profile profile in info.Data.Profiles)
                {
                    if (profile.Id != bot.Profile.Id)
                    {
                        continue;
                    }

                    matchingGroupData = info;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBotGroupFromAnyGenerator(BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            if (botSpawnInfoCache.ContainsKey(bot))
            {
                matchingGroupData = botSpawnInfoCache[bot];
                return true;
            }

            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (TryGetBotGroup(bot, out matchingGroupData) == true)
                {
                    botSpawnInfoCache.Add(bot, matchingGroupData);
                    return true;
                }
            }

            matchingGroupData = null!;
            return false;
        }
    }
}
