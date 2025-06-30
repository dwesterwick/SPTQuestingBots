using EFT;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public abstract class AbstractBotMonitor
    {
        protected BotOwner BotOwner { get; private set; }
        protected BotObjectiveManager ObjectiveManager { get; private set; }
        protected BotMonitorController BotMonitor { get; private set; }

        public AbstractBotMonitor(BotOwner _botOwner)
        {
            BotOwner = _botOwner;
            ObjectiveManager = BotOwner.GetOrAddObjectiveManager();
            BotMonitor = ObjectiveManager?.BotMonitor;
        }

        public virtual void Awake() { }

        public virtual void Update() { }

        public virtual void OnDestroy() { }
    }
}
