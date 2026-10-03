using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class NavigationPointSelectionConfig
    {
        [DataMember(Name = "max_search_distance_for_bosses", IsRequired = true)]
        public float MaxSearchDistanceForBosses { get; set; } = 50;

        [DataMember(Name = "max_search_distance_for_followers", IsRequired = true)]
        public float MaxSearchDistanceForFollowers { get; set; } = 15;

        [DataMember(Name = "min_search_distance_for_bosses")]
        public float MinSearchDistanceForBosses { get; set; } = 0;

        [DataMember(Name = "min_search_distance_for_followers")]
        public float MinSearchDistanceForFollowers { get; set; } = 0;

        [DataMember(Name = "debounce_time_after_checking")]
        public float DebounceTimeAfterChecking { get; set; } = 0;

        [DataMember(Name = "debounce_time_after_updating")]
        public float DebounceTimeAfterUpdating { get; set; } = 0;

        public NavigationPointSelectionConfig()
        {

        }
    }
}
