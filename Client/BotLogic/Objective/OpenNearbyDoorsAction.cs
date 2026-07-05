using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.Objective
{
    public class OpenNearbyDoorsAction : AbstractInteractWithNearbyDoorsAction
    {
        public OpenNearbyDoorsAction(BotOwner _BotOwner) : base(_BotOwner, EInteractionType.Open, false)
        {

        }
    }
}
