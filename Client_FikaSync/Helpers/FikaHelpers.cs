using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using QuestingBots.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class FikaHelpers
    {
        internal static bool TryGetGameWorld(out GameWorld? gameWorld)
        {
            gameWorld = Singleton<GameWorld>.Instance;
            return Singleton<GameWorld>.Instantiated;
        }

        internal static bool TryGetDebugData(out DebugData? debugData)
        {
            debugData = null;

            if (!TryGetGameWorld(out GameWorld? gameWorld) || (gameWorld == null))
            {
                return false;
            }

            if (!gameWorld.TryGetComponent(out debugData) || (debugData == null))
            {
                return false;
            }

            return true;
        }

        internal static Player? GetBotPlayer(string name)
        {
            if (!Singleton<IFikaGame>.Instantiated)
            {
                return null;
            }

            if (Singleton<IFikaGame>.Instance.GameController.Bots.TryGetValue(name, out Player player))
            {
                return player;
            }

            return null;
        }

        internal static FikaPlayer? GetPlayer(int netId)
        {
            if (!Singleton<IFikaGame>.Instantiated)
            {
                return null;
            }

            if (Singleton<IFikaGame>.Instance.GameController.CoopHandler.Players.TryGetValue(netId, out FikaPlayer player))
            {
                return player;
            }

            return null;
        }

        internal static FikaPlayer? GetMyPlayer()
        {
            if (!Singleton<IFikaGame>.Instantiated)
            {
                return null;
            }

            return Singleton<IFikaGame>.Instance.GameController.CoopHandler.MyPlayer;
        }

        internal static IEnumerable<FikaPlayer> GetPlayers()
        {
            if (!Singleton<IFikaGame>.Instantiated)
            {
                yield break;
            }

            foreach (FikaPlayer player in Singleton<IFikaGame>.Instance.GameController.CoopHandler.Players.Values)
            {
                yield return player;
            }
        }

        internal static IEnumerable<FikaPlayer> GetHumanPlayers()
        {
            if (!Singleton<IFikaGame>.Instantiated)
            {
                yield break;
            }

            foreach (FikaPlayer player in Singleton<IFikaGame>.Instance.GameController.CoopHandler.HumanPlayers)
            {
                yield return player;
            }
        }
    }
}
