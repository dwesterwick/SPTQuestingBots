using EFT.Game.Spawning;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.Models
{
    public class QuestSpawnPointObjective : QuestObjective
    {
        public SpawnPointParams? SpawnPoint { get; set; } = null;

        public QuestSpawnPointObjective() : base()
        {

        }

        public QuestSpawnPointObjective(SpawnPointParams spawnPoint, Vector3 position) : this()
        {
            SpawnPoint = spawnPoint;
            Position = position;
        }

        public override void Clear()
        {
            SpawnPoint = null;
            base.Clear();
        }

        public override string ToString()
        {
            if (SpawnPoint.HasValue)
            {
                return "Spawn Point " + Position.ToString();
            }

            return base.ToString();
        }
    }
}
