using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class QuestSettingsConfig
    {
        [DataMember(Name = "desirability", EmitDefaultValue = false, IsRequired = true)]
        public float Desirability { get; set; } = 50;

        [DataMember(Name = "pmcsOnly", EmitDefaultValue = false)]
        public bool PMCsOnly { get; set; } = false;

        [DataMember(Name = "max_bots_per_quest", EmitDefaultValue = false)]
        public int MaxBotsPerQuest { get; set; } = 10;

        [DataMember(Name = "min_distance", EmitDefaultValue = false)]
        public float MinDistance { get; set; } = 10;

        [DataMember(Name = "max_distance", EmitDefaultValue = false)]
        public float MaxDistance { get; set; } = 9999;

        [DataMember(Name = "max_raid_ET", EmitDefaultValue = false)]
        public float MaxRaidET { get; set; } = 999;

        [DataMember(Name = "chance_of_having_keys", EmitDefaultValue = false)]
        public float ChanceOfHavingKeys { get; set; } = 25;

        [DataMember(Name = "match_looting_behavior_distance", EmitDefaultValue = false)]
        public float MatchLootingBehaviorDistance { get; set; } = 0;

        [DataMember(Name = "min_level", EmitDefaultValue = false)]
        public int MinLevel { get; set; } = 0;

        [DataMember(Name = "max_level", EmitDefaultValue = false)]
        public int MaxLevel { get; set; } = 99;

        [DataMember(Name = "level_range", EmitDefaultValue = false)]
        public double[][] LevelRange { get; set; } = Array.Empty<double[]>();

        public QuestSettingsConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {
            PMCsOnly = false;
            MaxBotsPerQuest = 10;
            MinDistance = 10;
            MaxDistance = 9999;
            MaxRaidET = 999;
            ChanceOfHavingKeys = 25;
            MatchLootingBehaviorDistance = 0;
            MinLevel = 0;
            MaxLevel = 99;
            LevelRange = Array.Empty<double[]>();
        }
    }
}
