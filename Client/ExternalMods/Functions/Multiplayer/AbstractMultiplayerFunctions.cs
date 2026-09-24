using Comfort.Common;
using EFT;
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
            if (Singleton<GameWorld>.Instance == null)
            {
                yield break;
            }

            foreach (Player player in Singleton<GameWorld>.Instance.AllAlivePlayersList)
            {
                if (player.IsAI || !player.HealthController.IsAlive)
                {
                    continue;
                }

                yield return player;
            }
        }
    }
}
