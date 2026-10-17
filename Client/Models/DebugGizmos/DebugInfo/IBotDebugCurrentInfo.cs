using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Models.DebugGizmos.DebugInfo
{
    public interface IBotDebugCurrentInfo : IBotDebugInfo
    {
        public EBotState BotState { get; }
        public bool IsSleeping { get; }
        public int[] EnemyPlayerIds { get; }
        public string ActiveLayerName { get; }
        public string ActiveLogicName { get; }
        public bool HasCurrentEnemy { get; }
        public int CurrentEnemyPlayerId { get; }
        public bool HasBoss { get; }
        public int BossPlayerId { get; }
    }
}
