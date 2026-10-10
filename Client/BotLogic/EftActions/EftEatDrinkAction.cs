using DrakiaXYZ.BigBrain.Brains;
using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.BotLogic.EftActions
{
    internal class EftEatDrinkAction : AbstractEftBrainAction
    {
        public EftEatDrinkAction(BotOwner botOwner) : base(botOwner, BotLogicDecision.eatDrink)
        {

        }

        protected override void Update_CustomLogic(CustomLayer.ActionData data)
        {
            BotOwner.Mover.Stop();
        }
    }
}
