using EFT;
using QuestingBots.ExternalMods;
using QuestingBots.ExternalMods.Functions.Multiplayer;
using QuestingBots.Models.DebugGizmos.DebugInfo;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.Components
{
    public class PlayerData : MonoBehaviour
    {
        private AbstractMultiplayerFunctions multiPlayerFunctions = null!;
        private Dictionary<int, IClientBotDebugStaticInfo> clientBotInfo = new Dictionary<int, IClientBotDebugStaticInfo>();

        public Player? GetPlayer(int netId) => multiPlayerFunctions.GetPlayer(netId);
        public IEnumerable<Player> GetAllAlivePlayers() => multiPlayerFunctions.GetAllAlivePlayers();
        public IEnumerable<Player> GetAllAliveHumanPlayers() => multiPlayerFunctions.GetAllAliveHumanPlayers();

        protected void Awake()
        {
            multiPlayerFunctions = ExternalModHandler.CreateMultiplayerFunctions();
        }

        public void RegisterClientBot(Player bot, IClientBotDebugStaticInfo info)
        {
            if (clientBotInfo.ContainsKey(bot.Id))
            {
                return;
            }

            clientBotInfo.Add(bot.Id, info);
        }

        public string GetBotNameText(Player bot)
        {
            if (!clientBotInfo.ContainsKey(bot.Id))
            {
                return bot.name;
            }

            return $"Bot{clientBotInfo[bot.Id].BotId}";
        }
    }
}
