using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Helpers
{
    public static class FikaHelpers
    {
        internal static bool TryGetGameWorld(out GameWorld? gameWorld)
        {
            gameWorld = null;

            if (Singleton<GameWorld>.Instance == null)
            {
                QuestingBotsFikaSyncPlugin.PluginLogger.LogError("GameWorld is null");
                return false;
            }

            gameWorld = Singleton<GameWorld>.Instance;

            return true;
        }

        internal static Player? GetPlayer(string name)
        {
            if (Singleton<IFikaGame>.Instance.GameController.Bots.TryGetValue(name, out Player player))
            {
                return player;
            }

            return null;
        }

        internal static IEnumerable<Player> GetCoopPlayers()
        {
            foreach (FikaPlayer player in Singleton<IFikaGame>.Instance.GameController.CoopHandler.HumanPlayers)
            {
                yield return player;
            }
        }

        internal static IEnumerable<Vector3> GetCoopPlayerPositions()
        {
            foreach (Player player in GetCoopPlayers())
            {
                yield return player.Position;
            }
        }
    }
}
