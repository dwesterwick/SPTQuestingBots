using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.EftActions
{
    internal class EftHealAction : AbstractEftBrainAction
    {
        public EftHealAction(BotOwner botOwner) : base(botOwner, BotLogicDecision.heal)
        {

        }
    }
}
