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
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private int updateInterval = 100;

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

            updateTimer.Restart();
            return true;
        }
    }
}
