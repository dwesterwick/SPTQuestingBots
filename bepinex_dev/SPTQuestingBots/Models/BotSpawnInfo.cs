using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using QuestingBots.Controllers;
using UnityEngine;

namespace QuestingBots.Models
{
    public class BotSpawnInfo
    {
        public GClass628 Data { get; private set; }
        public BotOwner[] Owners { get; set; }
        public SpawnPointParams? SpawnPoint { get; set; }
        public Vector3[] SpawnPositions { get; set; } = new Vector3[0];

        public BotSpawnInfo(GClass628 data)
        {
            Data = data;
        }

        public void AssignSpawnPositionsFromSpawnPoint(int botCount)
        {
            List<SpawnPointParams> spawnPoints = new List<SpawnPointParams>();
            if (SpawnPoint.HasValue)
            {
                Vector3 mainSpawnPosition = SpawnPoint.Value.Position.ToUnityVector3();
                spawnPoints.Add(SpawnPoint.Value);
                int positionsGenerated = 1;
                while (positionsGenerated < botCount)
                {
                    SpawnPointParams nextPosition = BotGenerator.GetNearestSpawnPoint(mainSpawnPosition, spawnPoints.ToArray());
                }

                SpawnPositions = spawnPoints.Select(p => p.Position.ToUnityVector3()).ToArray();
                return;
            }

            throw new InvalidOperationException("A spawn point has not been assigned to the bot.");
        }
    }
}
