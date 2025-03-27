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
        protected GClass168 baseAction { get; private set; } = null;

        public ParalyzeAction(BotOwner _BotOwner) : base(_BotOwner)
        {
            // This doesn't quite achieve "paralysis", but it's probably good enough
            baseAction = GClass522.CreateNode(BotLogicDecision.standBy, BotOwner);
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

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            baseAction.UpdateNodeByMain(data);
        }
    }
}
