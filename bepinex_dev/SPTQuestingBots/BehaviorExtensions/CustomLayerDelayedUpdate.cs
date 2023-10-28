using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal abstract class CustomLayerDelayedUpdate : CustomLayer
    {
        protected bool previousState { get; private set; } = false;
        protected bool wasStuck = false;

        private Stopwatch updateTimer = Stopwatch.StartNew();
        private Stopwatch botIsStuckTimer = Stopwatch.StartNew();
        private Stopwatch pauseLayerTimer = Stopwatch.StartNew();
        private float pauseLayerTime = 0;
        private int updateInterval = 100;

        protected double StuckTime => botIsStuckTimer.ElapsedMilliseconds / 1000.0;

        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority) : base(_botOwner, _priority)
        {

        }

        public CustomLayerDelayedUpdate(BotOwner _botOwner, int _priority, int delayInterval) : this(_botOwner, _priority)
        {
            updateInterval = delayInterval;
        }

        protected bool canUpdate()
        {
            if (updateTimer.ElapsedMilliseconds < updateInterval)
            {
                return false;
            }

            if (pauseLayerTimer.ElapsedMilliseconds < 1000 * pauseLayerTime)
            {
                return false;
            }

            updateTimer.Restart();
            return true;
        }

        protected bool updatePreviousState(bool newState)
        {
            previousState = newState;
            return previousState;
        }

        protected bool pauseLayer()
        {
            return pauseLayer(0);
        }

        protected bool pauseLayer(float minTime)
        {
            pauseLayerTime = minTime;
            pauseLayerTimer.Restart();

            restartStuckTimer();

            return false;
        }

        protected void restartStuckTimer()
        {
            botIsStuckTimer.Restart();
        }
    }
}
