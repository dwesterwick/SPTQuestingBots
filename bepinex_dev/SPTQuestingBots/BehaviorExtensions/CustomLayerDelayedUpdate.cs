using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal class CustomLayerDelayedUpdate : CustomLayer
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

        public override string GetName()
        {
            throw new NotImplementedException();
        }

        public override Action GetNextAction()
        {
            throw new NotImplementedException();
        }

        public override bool IsActive()
        {
            throw new NotImplementedException();
        }

        public override bool IsCurrentActionEnding()
        {
            throw new NotImplementedException();
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
