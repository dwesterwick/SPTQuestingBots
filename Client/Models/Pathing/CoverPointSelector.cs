using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Models.Pathing
{
    public class CoverPointSelector
    {
        private BotOwner _bot;

        public Vector3? CoverPoint => null;

        public CoverPointSelector(BotOwner bot)
        {
            _bot = bot;
        }

        public void RefreshCoverPoint()
        {

        }
    }
}
