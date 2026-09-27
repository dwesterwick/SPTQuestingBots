using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class MaxAliveBotsConfig
    {
        [DataMember(Name = "use_map_lobby_size", IsRequired = true)]
        public bool UseMapLobbySize { get; set; } = false;

        [DataMember(Name = "overrides_if_not_using_lobby_size", IsRequired = true)]
        public Dictionary<string, int> OverridesIfNotUsingLobbySize { get; set; } = new Dictionary<string, int>();

        public MaxAliveBotsConfig()
        {

        }
    }
}
