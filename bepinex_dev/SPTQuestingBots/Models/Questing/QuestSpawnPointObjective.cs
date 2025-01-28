using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Game.Spawning;
using UnityEngine;

namespace SPTQuestingBots.Models.Questing
{
    public class QuestSpawnPointObjective : QuestObjective
    {
        public SpawnPointParams? SpawnPoint { get; set; } = null;

        public QuestSpawnPointObjective() : base()
        {

        }

        public QuestSpawnPointObjective(SpawnPointParams spawnPoint, Vector3 position) : base(position)
        {
            SpawnPoint = spawnPoint;
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
                return "Spawn Point " + (this.GetFirstStepPosition()?.ToString() ?? "???");
            }

            return base.ToString();
        }
    }
}
