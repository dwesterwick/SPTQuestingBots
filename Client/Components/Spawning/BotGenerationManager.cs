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
        private Dictionary<string, Models.BotSpawnInfo> botSpawnInfoCache = new Dictionary<string, Models.BotSpawnInfo>();

        public void AddActiveBotGenerator(BotGenerator botGenerator)
        {
            activeBotGenerators.Add(botGenerator);
        }

        public BotGenerator? GetActiveBotGenerator<T>() where T: BotGenerator
        {
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (botGenerator is T)
                {
                    return botGenerator;
                }
            }

            return null;
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
            foreach (Profile profile in GetAllGeneratedBotProfiles())
            {
                yield return profile.Id;
            }
        }

        public IEnumerable<Profile> GetAllGeneratedBotProfiles()
        {
            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (botGenerator == null)
                {
                    continue;
                }

                foreach (Profile profile in GetGeneratedBotProfiles(botGenerator))
                {
                    yield return profile;
                }
            }
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
                foreach (BotOwner bot in botGroup.SpawnedBots)
                {
                    if ((bot == null) || bot.IsDead)
                    {
                        continue;
                    }

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

        public IEnumerable<string> GetGeneratedBotProfileIDs(BotGenerator botGenerator)
        {
            foreach (Profile profile in GetGeneratedBotProfiles(botGenerator))
            {
                yield return profile.Id;
            }
        }

        public IEnumerable<Profile> GetGeneratedBotProfiles(BotGenerator botGenerator)
        {
            foreach (Models.BotSpawnInfo botGroup in botGenerator.GetBotGroups())
            {
                foreach (Profile profile in botGroup.Data.Profiles)
                {
                    yield return profile;
                }
            }
        }

        public bool TryGetBotGroup(BotGenerator botGenerator, BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            return TryGetBotGroup(botGenerator, bot.Profile.Id, out matchingGroupData);
        }

        public bool TryGetBotGroup(BotGenerator botGenerator, string profileId, out Models.BotSpawnInfo matchingGroupData)
        {
            if (botSpawnInfoCache.ContainsKey(profileId))
            {
                matchingGroupData = botSpawnInfoCache[profileId];
                return true;
            }

            matchingGroupData = null!;

            foreach (Models.BotSpawnInfo info in botGenerator.GetBotGroups())
            {
                foreach (Profile profile in info.Data.Profiles)
                {
                    if (profile.Id != profileId)
                    {
                        continue;
                    }

                    matchingGroupData = info;
                    botSpawnInfoCache.Add(profileId, matchingGroupData);

                    return true;
                }
            }

            return false;
        }

        public bool TryGetBotGroupFromAnyGenerator(BotOwner bot, out Models.BotSpawnInfo matchingGroupData)
        {
            return TryGetBotGroupFromAnyGenerator(bot.Profile.Id, out matchingGroupData);
        }

        public bool TryGetBotGroupFromAnyGenerator(string profileId, out Models.BotSpawnInfo matchingGroupData)
        {
            if (botSpawnInfoCache.ContainsKey(profileId))
            {
                matchingGroupData = botSpawnInfoCache[profileId];
                return true;
            }

            foreach (BotGenerator botGenerator in activeBotGenerators)
            {
                if (TryGetBotGroup(botGenerator, profileId, out matchingGroupData) == true)
                {
                    return true;
                }
            }

            matchingGroupData = null!;
            return false;
        }
    }
}
