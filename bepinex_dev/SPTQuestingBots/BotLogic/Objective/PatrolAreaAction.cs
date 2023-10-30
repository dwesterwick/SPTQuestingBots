using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.Objective
{
    public class PatrolAreaAction : BehaviorExtensions.CustomLogicDelayedUpdate
    {
        private GClass114 baseAction = null;

        public PatrolAreaAction(BotOwner _BotOwner) : base(_BotOwner, 1)
        {
            baseAction = GClass394.CreateNode(BotLogicDecision.simplePatrol, BotOwner);
            baseAction.Awake();
        }

        public override void Start()
        {
            BotOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            baseAction.Update();
        }
    }
}
