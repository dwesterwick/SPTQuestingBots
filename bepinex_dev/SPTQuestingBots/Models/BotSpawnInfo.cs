using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using EFT.Game.Spawning;
using UnityEngine;

namespace QuestingBots.Models
{
    public class BotSpawnInfo
    {
        public GClass628 Data { get; private set; }
        public BotOwner Owner { get; set; }
        public SpawnPointParams? SpawnPoint { get; set; }
        public Vector3? SpawnPosition { get; set; }

        public BotSpawnInfo(GClass628 data)
        {
            Data = data;
        }

        public void AssignSpawnPositionFromSpawnPoint()
        {
            if (SpawnPoint.HasValue)
            {
                SpawnPosition = SpawnPoint.Value.Position.ToUnityVector3();
                return;
            }

            throw new InvalidOperationException("A spawn point has not been assigned to the bot.");
        }
    }
}
