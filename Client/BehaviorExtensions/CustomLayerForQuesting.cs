using Comfort.Common;
using EFT;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BehaviorExtensions
{
    internal abstract class CustomLayerForQuesting : CustomLayerDelayedUpdate
    {
        protected Components.BotObjectiveManager ObjectiveManager { get; private set; } = null!;

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority, int delayInterval) : base(_botOwner, _priority, delayInterval)
        {
            Components.BotObjectiveManager? objectiveManager = _botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not get BotObjectiveManager for " + _botOwner.GetText());
                return;
            }

            ObjectiveManager = objectiveManager;
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
            float pauseTime = ObjectiveManager.PauseRequest;
            ObjectiveManager.PauseRequest = 0;

            return pauseTime;
        }
    }
}
