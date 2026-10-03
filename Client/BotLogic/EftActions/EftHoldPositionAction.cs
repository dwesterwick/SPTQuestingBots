using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.EftActions
{
    internal class EftHoldPositionAction : AbstractEftBrainAction
    {
        public EftHoldPositionAction(BotOwner botOwner) : base(botOwner, BotLogicDecision.holdPosition)
        {

        }
    }
}
