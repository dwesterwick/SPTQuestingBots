using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models
{
    public class BotHeardSoundData
    {
        public float Time { get; private set; }
        public float Loundness { get; private set; }
        public IPlayer EnemyPlayer { get; private set; }
        public Vector3? EstimatedPosition { get; private set; } = null;
        public float? PositionError { get; private set; } = null;

        public BotHeardSoundData(float time, float loudness, IPlayer enemyPlayer)
        {
            Time = time;
            Loundness = loudness;
            EnemyPlayer = enemyPlayer;
        }

        public BotHeardSoundData(float time, float loudness, IPlayer enemyPlayer, Vector3? estimatedPosition, float? positionError) : this(time, loudness, enemyPlayer)
        {
            EstimatedPosition = estimatedPosition;
            PositionError = positionError;
        }
    }
}
