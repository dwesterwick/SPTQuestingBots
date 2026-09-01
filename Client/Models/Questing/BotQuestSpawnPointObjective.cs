using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT.Game.Spawning;
using UnityEngine;

namespace QuestingBots.Models.Questing
{
    public class BotQuestSpawnPointObjective : BotQuestObjective
    {
        public SpawnPointParams? SpawnPoint { get; set; } = null;

        public BotQuestSpawnPointObjective() : base()
        {

        }

        public BotQuestSpawnPointObjective(SpawnPointParams spawnPoint, Vector3 position) : base(position)
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
