using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BehaviorExtensions
{
    internal abstract class CustomLayerForQuesting : CustomLayerDelayedUpdate
    {
        protected Components.BotObjectiveManager objectiveManager { get; private set; } = null;

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority, int delayInterval) : base(_botOwner, _priority, delayInterval)
        {
            objectiveManager = _botOwner.GetOrAddObjectiveManager();
        }

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority) : this(_botOwner, _priority, updateInterval)
        {

        }

        public override Action GetNextAction()
        {
            return base.GetNextAction();
        }

        public override bool IsCurrentActionEnding()
        {
            return base.IsCurrentActionEnding();
        }

        protected float getPauseRequestTime()
        {
            float pauseTime = objectiveManager.PauseRequest;
            objectiveManager.PauseRequest = 0;

            return pauseTime;
        }
    }
}
