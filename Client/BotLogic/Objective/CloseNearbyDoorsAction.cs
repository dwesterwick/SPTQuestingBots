using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Objective
{
    internal class CloseNearbyDoorsAction : AbstractInteractWithNearbyDoorsAction
    {
        public CloseNearbyDoorsAction(BotOwner _BotOwner) : base(_BotOwner, EInteractionType.Close, false)
        {

        }
    }
}
