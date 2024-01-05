using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotSpawnInfo
    {
        public bool HasSpawned { get; set; } = false;

        public int GroupNumber { get; private set; }
        public GClass513 Data { get; private set; }
        public List<BotOwner> Owners { get; set; } = new List<BotOwner>();
        public SpawnPointParams? SpawnPoint { get; set; }
        public Vector3[] SpawnPositions { get; set; } = new Vector3[0];

        // The key should be Profile.Id for each bot that's generated
        private Dictionary<string, WildSpawnType> OriginalBotSpawnTypes = new Dictionary<string, WildSpawnType>();

        public int Count => Data?.Profiles?.Count ?? 0;

        public BotSpawnInfo(int groupNum, GClass513 data)
        {
            GroupNumber = groupNum;
            Data = data;
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

        public bool TryAssignFurthestSpawnPoint(ESpawnCategoryMask allowedSpawnPointTypes, string[] blacklistedSpawnPointIDs)
        {
            // Enumerate all valid spawn points
            SpawnPointParams[] validSpawnPoints = LocationController.GetAllValidSpawnPointParams()
                    .Where(s => !blacklistedSpawnPointIDs.Contains(s.Id))
                    .Where(s => s.Categories.Any(allowedSpawnPointTypes))
                    .ToArray();
            
            if (validSpawnPoints.Length == 0)
            {
                return false;
            }

            // Get the locations of all alive bots/players on the map. If the count is 0, you're dead so there's no reason to spawn more bots.
            Vector3[] playerPositions = Singleton<GameWorld>.Instance.AllAlivePlayersList.Select(s => s.Position).ToArray();
            if (playerPositions.Length == 0)
            {
                return false;
            }

            SpawnPoint = LocationController.GetFurthestSpawnPoint(playerPositions, validSpawnPoints);
            if (SpawnPoint == null)
            {
                return false;
            }

            LoggingController.LogInfo("Valid spawn points: " + validSpawnPoints.Length);
            LoggingController.LogInfo("Main player position: " + Singleton<GameWorld>.Instance.MainPlayer.Position.ToString());
            LoggingController.LogInfo("Existing player positions: " + string.Join(", ", playerPositions.Select(p => p.ToString())));
            
            return true;
        }

        public void AssignSpawnPositionsFromSpawnPoint(int botCount)
        {
            AssignSpawnPositionsFromSpawnPoint(botCount, new SpawnPointParams[0]);
        }

        public void AssignSpawnPositionsFromSpawnPoint(int botCount, SpawnPointParams[] excludedSpawnPoints)
        {
            if (botCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(botCount), "Bot count must be at least 1.");
            }

            if (!SpawnPoint.HasValue)
            {
                throw new InvalidOperationException("A spawn point has not been assigned to the bot group.");
            }
            Vector3 mainSpawnPosition = SpawnPoint.Value.Position.ToUnityVector3();

            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>() { SpawnPoint.Value };

            // If there are multiple bots that will spawn, select nearby spawn points for each of them
            while (spawnPoints.Count < botCount)
            {
                SpawnPointParams nextPosition = LocationController.GetNearestSpawnPoint(mainSpawnPosition, spawnPoints.ToArray().AddRangeToArray(excludedSpawnPoints));

                Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(nextPosition.Position, ConfigController.Config.Questing.QuestGeneration.NavMeshSearchDistanceSpawn);
                if (!navMeshPosition.HasValue)
                {
                    excludedSpawnPoints = excludedSpawnPoints.AddItem(nextPosition).ToArray();
                    continue;
                }

                spawnPoints.Add(nextPosition);
            }

            SpawnPositions = spawnPoints.Select(p => p.Position.ToUnityVector3()).ToArray();
        }
    }
}
