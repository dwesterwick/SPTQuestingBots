using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class BotZoneUpdatesConfig
    {
        [DataMember(Name = "update_bot_zone_after_stopping", IsRequired = true)]
        public bool UpdateBotZoneAfterStopping { get; set; } = true;

        [DataMember(Name = "patrol_point_updates", IsRequired = true)]
        public NavigationPointSelectionConfig PatrolPointUpdates { get; set; } = new NavigationPointSelectionConfig();

        [DataMember(Name = "cover_point_updates", IsRequired = true)]
        public NavigationPointSelectionConfig CoverPointUpdates { get; set; } = new NavigationPointSelectionConfig();

        public BotZoneUpdatesConfig()
        {

        }
    }
}
