using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.BotLogic.Sleep
{
    internal class SleepingAction : CustomLogic
    {
        public SleepingAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            
        }

        public override void Start()
        {
            base.Start();

            BotOwner.DecisionQueue.Clear();
            BotOwner.Memory.GoalEnemy = null;
            BotOwner.PatrollingData.Pause();

            BotRegistrationManager.RegisterSleepingBot(BotOwner);
            BotOwner.gameObject.SetActive(false);
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.gameObject.SetActive(true);
            BotRegistrationManager.UnregisterSleepingBot(BotOwner);

            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            return;
        }
    }
}
