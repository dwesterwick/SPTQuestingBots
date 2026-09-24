using Comfort.Common;
using EFT;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace QuestingBots.ExternalMods.Functions.Multiplayer
{
    public class FikaMultiplayerFunctions : AbstractMultiplayerFunctions
    {
        public static void SetGetPayersFunc(Func<IEnumerable<Player>> func) => _getPlayersFunc = func;
        private static Func<IEnumerable<Player>>? _getPlayersFunc = null;

        public FikaMultiplayerFunctions() : base()
        {

        }

        public override IEnumerable<Player> GetHumanPlayers()
        {
            if (_getPlayersFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Fika sync plugin did not register a GetHumanPlayers function. Using base function.");

                return base.GetHumanPlayers();
            }

            return _getPlayersFunc();
        }
    }
}
