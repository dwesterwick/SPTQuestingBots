using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic
{
    public class HoldAtObjectiveAction : CustomLogic
    {
        private BotOwner botOwner;
        private Stopwatch updateTimer = Stopwatch.StartNew();
        private GClass104 baseAction = null;
        private int updateInterval = 10;

        public HoldAtObjectiveAction(BotOwner _botOwner) : base(_botOwner)
        {
            botOwner = _botOwner;

            baseAction = GClass475.CreateNode(BotLogicDecision.holdPosition, botOwner);
        }

        public override void Start()
        {
            baseAction.Awake();

            botOwner.PatrollingData.Pause();
        }

        public override void Stop()
        {
            botOwner.PatrollingData.Unpause();
        }

        public override void Update()
        {
            if (updateTimer.ElapsedMilliseconds < updateInterval)
            {
                return;
            }
            updateTimer.Restart();

            baseAction.Update();
        }
    }
}
