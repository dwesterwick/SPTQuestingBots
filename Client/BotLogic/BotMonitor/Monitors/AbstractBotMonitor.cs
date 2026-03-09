using EFT;
using QuestingBots.Components;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.BotMonitor.Monitors
{
    public abstract class AbstractBotMonitor
    {
        protected BotOwner BotOwner { get; private set; } = null!;
        protected BotObjectiveManager ObjectiveManager { get; private set; } = null!;
        protected BotMonitorController BotMonitor { get; private set; } = null!;

        public AbstractBotMonitor(BotOwner _botOwner)
        {
            if (_botOwner == null)
            {
                throw new ArgumentNullException(nameof(_botOwner), "BotOwner cannot be null");
            }

            BotOwner = _botOwner;
            ObjectiveManager = BotOwner.GetOrAddObjectiveManager();

            if (ObjectiveManager != null)
            {
                BotMonitor = ObjectiveManager.BotMonitor;
            }
        }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void OnDestroy() { }
    }
}
