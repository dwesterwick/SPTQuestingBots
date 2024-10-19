using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;

namespace SPTQuestingBots_CustomBotGenExample
{
    public class ParalyzeAction : CustomLogic
    {
        protected GClass134 baseAction { get; private set; } = null;

        public ParalyzeAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            // This doesn't quite achieve "paralysis", but it's probably good enough
            baseAction = GClass459.CreateNode(BotLogicDecision.standBy, BotOwner);
            baseAction.Awake();
        }

        public override void Start()
        {
            BotOwner.PatrollingData.Pause();
            BotOwner.Mover.Stop();
        }

        public override void Stop()
        {
            BotOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            baseAction.Update();
        }
    }
}
