using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class ModConfig
    {
        [DataMember(Name = "enabled", EmitDefaultValue = false, IsRequired = true)]
        public bool Enabled { get; set; } = true;

        [DataMember(Name = "debug", EmitDefaultValue = false, IsRequired = true)]
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [DataMember(Name = "max_calc_time_per_frame_ms", EmitDefaultValue = false, IsRequired = true)]
        public float MaxCalcTimePerFrame { get; set; } = 5;

        [DataMember(Name = "chance_of_being_hostile_toward_bosses", EmitDefaultValue = false, IsRequired = true)]
        public BotTypeValueConfig ChanceOfBeingHostileTowardBosses { get; set; } = new BotTypeValueConfig();

        [DataMember(Name = "questing", EmitDefaultValue = false, IsRequired = true)]
        public QuestingConfig Questing { get; set; } = new QuestingConfig();

        [DataMember(Name = "bot_spawns", EmitDefaultValue = false, IsRequired = true)]
        public BotSpawnsConfig BotSpawns { get; set; } = new BotSpawnsConfig();

        [DataMember(Name = "adjust_pscav_chance", EmitDefaultValue = false, IsRequired = true)]
        public AdjustPScavChanceConfig AdjustPScavChance { get; set; } = new AdjustPScavChanceConfig();

        public ModConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
