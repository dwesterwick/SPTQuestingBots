using Comfort.Common;
using EFT;
using Fika.Core.Main.GameMode;
using Fika.Core.Main.Players;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots
{
    public static class FikaHelpers
    {
        public static IEnumerable<Player> GetCoopPlayers()
        {
            foreach (FikaPlayer player in Singleton<IFikaGame>.Instance.GameController.CoopHandler.HumanPlayers)
            {
                yield return player;
            }
        }

        public static IEnumerable<Vector3> GetCoopPlayerPositions()
        {
            foreach (Player player in GetCoopPlayers())
            {
                yield return player.Position;
            }
        }
    }
}
