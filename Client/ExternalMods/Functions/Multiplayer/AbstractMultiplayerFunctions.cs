using Comfort.Common;
using EFT;
using QuestingBots.ExternalMods.Functions;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.ExternalMods.Functions.Multiplayer
{
    public abstract class AbstractMultiplayerFunctions : IAbstractBaseExternalFunction
    {
        public AbstractMultiplayerFunctions()
        {

        }

        public virtual IEnumerable<Player> GetHumanPlayers()
        {
            yield return Singleton<GameWorld>.Instance.MainPlayer;
        }
    }
}
