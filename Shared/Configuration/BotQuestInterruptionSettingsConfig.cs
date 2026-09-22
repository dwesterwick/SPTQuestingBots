using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace QuestingBots.Models.Questing
{
    [DataContract]
    public class BotQuestInterruptionSettingsConfig
    {
        [DataMember(Name = "enabled")]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "chance_per_distance")]
        public double[][] ChancePerDistance { get; set; } = Array.Empty<double[]>();

        public BotQuestInterruptionSettingsConfig()
        {

        }
    }
}
