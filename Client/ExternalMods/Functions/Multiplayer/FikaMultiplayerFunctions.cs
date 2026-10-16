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
        public static void SetGetPayerFunc(Func<int, Player?> func) => _getPlayerFunc = func;
        private static Func<int, Player?>? _getPlayerFunc = null;

        public static void SetGetMyPayerFunc(Func<Player?> func) => _getMyPlayerFunc = func;
        private static Func<Player?>? _getMyPlayerFunc = null;

        public static void SetGetAlivePayersFunc(Func<IEnumerable<Player>> func) => _getPlayersFunc = func;
        private static Func<IEnumerable<Player>>? _getPlayersFunc = null;

        public static void SetGetAliveHumanPayersFunc(Func<IEnumerable<Player>> func) => _getHumanPlayersFunc = func;
        private static Func<IEnumerable<Player>>? _getHumanPlayersFunc = null;

        public FikaMultiplayerFunctions() : base()
        {

        }

        public override Player? GetPlayer(int netId)
        {
            if (_getPlayerFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Fika sync plugin did not register a GetPlayer function. Using base function.");

                return base.GetPlayer(netId);
            }

            return _getPlayerFunc(netId);
        }

        public override Player? GetMyPlayer()
        {
            if (_getMyPlayerFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Fika sync plugin did not register a GetMyPlayer function. Using base function.");

                return base.GetMyPlayer();
            }

            return _getMyPlayerFunc();
        }

        public override IEnumerable<Player> GetAllAlivePlayers()
        {
            if (_getPlayersFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Fika sync plugin did not register a GetAlivePayers function. Using base function.");

                return base.GetAllAlivePlayers();
            }

            return _getPlayersFunc();
        }

        public override IEnumerable<Player> GetAllAliveHumanPlayers()
        {
            if (_getHumanPlayersFunc == null)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Fika sync plugin did not register a GetAliveHumanPayers function. Using base function.");

                return base.GetAllAliveHumanPlayers();
            }

            return _getHumanPlayersFunc();
        }
    }
}
