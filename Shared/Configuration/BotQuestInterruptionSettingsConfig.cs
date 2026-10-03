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

        [DataMember(Name = "ignore_desirability")]
        public bool IgnoreDesirability { get; set; } = false;

        [DataMember(Name = "quest_expiration_after_first_trigger")]
        public float QuestExpirationAfterFirstTrigger { get; set; } = float.MaxValue;

        [DataMember(Name = "chance_per_distance")]
        public double[][] ChancePerDistance { get; set; } = Array.Empty<double[]>();

        public BotQuestInterruptionSettingsConfig()
        {

        }
    }
}
