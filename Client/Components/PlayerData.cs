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
        private Dictionary<int, IBotDebugStaticInfo> botInfo = new Dictionary<int, IBotDebugStaticInfo>();

        public Player? GetPlayer(int netId) => multiPlayerFunctions.GetPlayer(netId);
        public Player? GetMyPlayer() => multiPlayerFunctions.GetMyPlayer();
        public IEnumerable<Player> GetAllAlivePlayers() => multiPlayerFunctions.GetAllAlivePlayers();
        public IEnumerable<Player> GetAllAliveHumanPlayers() => multiPlayerFunctions.GetAllAliveHumanPlayers();

        protected void Awake()
        {
            multiPlayerFunctions = ExternalModHandler.CreateMultiplayerFunctions();
        }

        public void RegisterBot(Player bot, IBotDebugStaticInfo info)
        {
            if (botInfo.ContainsKey(bot.Id))
            {
                return;
            }

            botInfo.Add(bot.Id, info);
        }

        public string GetBotNameText(Player bot)
        {
            if (!botInfo.ContainsKey(bot.Id))
            {
                return bot.name;
            }

            return $"Bot{botInfo[bot.Id].BotId}";
        }
    }
}
