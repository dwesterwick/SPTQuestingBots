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
        public GClass628 Data { get; private set; }
        public BotOwner[] Owners { get; set; } = new BotOwner[0];
        public SpawnPointParams? SpawnPoint { get; set; }
        public Vector3[] SpawnPositions { get; set; } = new Vector3[0];

        public BotSpawnInfo(GClass628 data)
        {
            Data = data;
        }

        public bool TryAssignFurthestSpawnPoint(ESpawnCategoryMask allowedSpawnPointTypes, string[] blacklistedSpawnPointIDs)
        {
            SpawnPointParams[] validSpawnPoints = LocationController.CurrentLocation.SpawnPointParams
                    .Where(s => !blacklistedSpawnPointIDs.Contains(s.Id))
                    .Where(s => s.Categories.Any(allowedSpawnPointTypes))
                    .ToArray();
            if (validSpawnPoints.Length == 0)
            {
                return false;
            }

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

            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>();
            if (SpawnPoint.HasValue)
            {
                Vector3 mainSpawnPosition = SpawnPoint.Value.Position.ToUnityVector3();
                spawnPoints.Add(SpawnPoint.Value);
                int positionsGenerated = 1;
                while (positionsGenerated < botCount)
                {
                    SpawnPointParams nextPosition = LocationController.GetNearestSpawnPoint(mainSpawnPosition, spawnPoints.ToArray().AddRangeToArray(excludedSpawnPoints));

                    Vector3? navMeshPosition = LocationController.FindNearestNavMeshPosition(nextPosition.Position, ConfigController.Config.QuestGeneration.NavMeshSearchDistanceSpawn);
                    if (!navMeshPosition.HasValue)
                    {
                        continue;
                    }

                    spawnPoints.Add(nextPosition);
                }

                SpawnPositions = spawnPoints.Select(p => p.Position.ToUnityVector3()).ToArray();
                return;
            }

            throw new InvalidOperationException("A spawn point has not been assigned to the bot group.");
        }
    }
}
