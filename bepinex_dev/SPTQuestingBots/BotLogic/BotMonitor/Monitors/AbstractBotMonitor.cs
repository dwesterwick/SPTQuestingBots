using EFT;
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

        public AbstractBotMonitor(BotOwner _botOwner)
        {
            BotOwner = _botOwner;
        }

        public virtual void Awake() { }

        public virtual void Update() { }

        public virtual void OnDestroy() { }
    }
}
