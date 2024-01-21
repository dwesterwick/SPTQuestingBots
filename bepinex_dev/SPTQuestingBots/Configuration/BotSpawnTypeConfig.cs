using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class BotSpawnTypeConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("fraction_of_max_players")]
        public float FractionOfMaxPlayers { get; set; } = 1;

        [JsonProperty("fraction_of_max_players_vs_raidET")]
        public double[][] FractionOfMaxPlayersVsRaidET { get; set; } = new double[0][];

        [JsonProperty("bots_per_group_distribution")]
        public double[][] BotsPerGroupDistribution { get; set; } = new double[0][];

        public BotSpawnTypeConfig()
        {

        }
    }
}
