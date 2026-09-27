using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class LobbySizeReductionConfig
    {
        [DataMember(Name = "always_use_full_lobbies", IsRequired = true)]
        public bool AlwaysUseFullLobbies { get; set; } = true;

        [DataMember(Name = "randomize_between_min_and_max_players", IsRequired = true)]
        public bool RandomizeBetweenMinAndMaxPlayers { get; set; } = true;

        [DataMember(Name = "nighttime_reductions")]
        public NighttimeReductionsConfig NighttimeReductions { get; set; } = new NighttimeReductionsConfig();

        [DataMember(Name = "chance_of_full_daytime_lobby", IsRequired = true)]
        public Dictionary<string, int> ChanceOfFullDaytimeLobby { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "min_percent_of_full_daytime_lobby", IsRequired = true)]
        public Dictionary<string, int> MinPercentOfFullDaytimeLobby { get; set; } = new Dictionary<string, int>();

        public LobbySizeReductionConfig()
        {

        }
    }
}
