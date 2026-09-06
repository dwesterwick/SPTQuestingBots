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
        protected BotOwner BotOwner { get; private set; }

        protected BotObjectiveManager? ObjectiveManager => BotOwner.GetObjectiveManager();
        protected BotMonitorController? BotMonitor => ObjectiveManager?.BotMonitor;

        public AbstractBotMonitor(BotOwner _botOwner)
        {
            if (_botOwner == null)
            {
                throw new ArgumentNullException(nameof(_botOwner), "BotOwner cannot be null");
            }

            BotOwner = _botOwner;
        }

        public virtual void Start() { }

        public virtual void UpdateAlways() { }
        public virtual void UpdateIfQuesting() { }
        
        public virtual void OnDestroy() { }
    }
}
