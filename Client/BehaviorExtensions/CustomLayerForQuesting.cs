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
        private Components.BotObjectiveManager? _objectiveManager = null;
        protected Components.BotObjectiveManager ObjectiveManager
        {
            get
            {
                if (_objectiveManager == null)
                {
                    _objectiveManager = BotOwner.GetObjectiveManager();
                }
                if (_objectiveManager == null)
                {
                    string errorMessage = this.GetType().Name + ": Could not get BotObjectiveManager for " + BotOwner.GetText();
                    //Singleton<LoggingUtil>.Instance.LogError(errorMessage);
                    throw new InvalidOperationException(errorMessage);
                }

                return _objectiveManager;
            }
        }

        public CustomLayerForQuesting(BotOwner _botOwner, int _priority, int delayInterval) : base(_botOwner, _priority, delayInterval)
        {

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
