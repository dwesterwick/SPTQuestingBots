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
        private Dictionary<int, IBotDebugStaticInfo> botDebugInfo = new Dictionary<int, IBotDebugStaticInfo>();

        public Player? GetPlayer(int netId) => multiPlayerFunctions.GetPlayer(netId);
        public Player? GetMyPlayer() => multiPlayerFunctions.GetMyPlayer();
        public IEnumerable<Player> GetAllAlivePlayers() => multiPlayerFunctions.GetAllAlivePlayers();
        public IEnumerable<Player> GetAllAliveHumanPlayers() => multiPlayerFunctions.GetAllAliveHumanPlayers();

        protected void Awake()
        {
            multiPlayerFunctions = ExternalModHandler.CreateMultiplayerFunctions();
        }

        public void RegisterBotDebugInfo(Player bot, IBotDebugStaticInfo info)
        {
            if (botDebugInfo.ContainsKey(bot.Id))
            {
                return;
            }

            botDebugInfo.Add(bot.Id, info);
        }

        public void IsRegistered(Player bot) => IsRegistered(bot.Id);
        public void IsRegistered(int botId) => botDebugInfo.ContainsKey(botId);

        public string GetBotNameText(Player bot)
        {
            if (!botDebugInfo.ContainsKey(bot.Id))
            {
                return bot.name;
            }

            return $"Bot{botDebugInfo[bot.Id].BotId}";
        }
    }
}
