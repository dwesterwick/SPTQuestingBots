using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotSpawnTypeConfig
    {
        [DataMember(Name = "enabled", IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "min_raid_time_remaining", IsRequired = true)]
        public float MinRaidTimeRemaining { get; set; } = 0;

        [DataMember(Name = "min_distance_from_players_initial", IsRequired = true)]
        public float MinDistanceFromPlayersInitial { get; set; } = 25;

        [DataMember(Name = "min_distance_from_players_during_raid", IsRequired = true)]
        public float MinDistanceFromPlayersDuringRaid { get; set; } = 100;

        [DataMember(Name = "min_distance_from_players_during_raid_factory", IsRequired = true)]
        public float MinDistanceFromPlayersDuringRaidFactory { get; set; } = 50;

        [DataMember(Name = "fraction_of_max_players")]
        public float FractionOfMaxPlayers { get; set; } = 1;

        [DataMember(Name = "time_randomness")]
        public float TimeRandomness { get; set; } = 0;

        [DataMember(Name = "fraction_of_max_players_vs_raidET")]
        public double[][] FractionOfMaxPlayersVsRaidET { get; set; } = Array.Empty<double[]>();

        [DataMember(Name = "bots_per_group_distribution", IsRequired = true)]
        public double[][] BotsPerGroupDistribution { get; set; } = Array.Empty<double[]>();

        [DataMember(Name = "bot_difficulty_as_online", IsRequired = true)]
        public double[][] BotDifficultyAsOnline { get; set; } = Array.Empty<double[]>();

        public BotSpawnTypeConfig()
        {
            
        }
    }
}
