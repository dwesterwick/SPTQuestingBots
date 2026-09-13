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

        [DataMember(Name = "update_patrol_point_after_stopping", IsRequired = true)]
        public bool UpdatePatrolPointAfterStopping { get; set; } = true;

        [DataMember(Name = "debounce_time_after_changing_patrol_point_s", IsRequired = true)]
        public float DebounceTimeAfterChangingPatrolPoint { get; set; } = 2;

        [DataMember(Name = "patrol_point_radius_around_boss", IsRequired = true)]
        public MinMaxConfig PatrolPointRadiusAroundBoss { get; set; } = new MinMaxConfig(3, 50);
    }
}
