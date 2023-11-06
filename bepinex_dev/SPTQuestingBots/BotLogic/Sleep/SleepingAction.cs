using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

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
            BotOwner.gameObject.SetActive(false);
        }

        public override void Stop()
        {
            base.Stop();

            BotOwner.gameObject.SetActive(true);
            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            return;
        }
    }
}
