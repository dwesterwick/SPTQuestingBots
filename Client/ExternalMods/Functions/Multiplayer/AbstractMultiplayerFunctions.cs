using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.ExternalMods.Functions.Multiplayer
{
    public abstract class AbstractMultiplayerFunctions : IAbstractBaseExternalFunction
    {
        public AbstractMultiplayerFunctions()
        {

        }

        public virtual Player? GetPlayer(int netId)
        {
            Singleton<GameWorld>.Instance.TryGetAlivePlayer(netId, out Player? player);
            return player;
        }

        public virtual Player? GetMyPlayer()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return null;
            }

            return Singleton<GameWorld>.Instance.MainPlayer;
        }

        public virtual IEnumerable<Player> GetAllAliveHumanPlayers()
        {
            foreach (Player player in GetAllAlivePlayers())
            {
                if (player.IsAI)
                {
                    continue;
                }

                yield return player;
            }
        }

        public virtual IEnumerable<Player> GetAllAlivePlayers()
        {
            if (Singleton<GameWorld>.Instance == null)
            {
                return Enumerable.Empty<Player>();
            }

            return Singleton<GameWorld>.Instance.AllAlivePlayersList;
        }
    }
}
