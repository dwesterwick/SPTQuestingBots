using EFT;
using QuestingBots.BehaviorExtensions;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.EftActions
{
    internal abstract class AbstractEftBrainAction : CustomLogicDelayedUpdate
    {
        public AbstractEftBrainAction(BotOwner botOwner, BotLogicDecision botLogicDecision) : this(botOwner, botLogicDecision, 0) { }

        public AbstractEftBrainAction(BotOwner botOwner, BotLogicDecision botLogicDecision, int delayInterval) : base(botOwner, delayInterval)
        {
            SetBaseAction(AIActionsList.CreateNode(botLogicDecision, BotOwner));
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            baseAction.UpdateNodeByMain(data);
        }
    }
}
