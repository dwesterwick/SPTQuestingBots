using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.Configuration
{
    [DataContract]
    public class HearingSensorConfig
    {
        [DataMember(Name = "enabled", EmitDefaultValue = false, IsRequired = true)]
        public bool Enabled { get; set; } = false;

        [DataMember(Name = "min_corrected_sound_power", EmitDefaultValue = false, IsRequired = true)]
        public float MinCorrectedSoundPower { get; set; } = 17;

        [DataMember(Name = "max_distance_footsteps", EmitDefaultValue = false, IsRequired = true)]
        public float MaxDistanceFootsteps { get; set; } = 20;

        [DataMember(Name = "max_distance_gunfire", EmitDefaultValue = false, IsRequired = true)]
        public float MaxDistanceGunfire { get; set; } = 75;

        [DataMember(Name = "max_distance_gunfire_suppressed", EmitDefaultValue = false, IsRequired = true)]
        public float MaxDistanceGunfireSuppressed { get; set; } = 75;

        [DataMember(Name = "loudness_multiplier_footsteps", EmitDefaultValue = false, IsRequired = true)]
        public float LoudnessMultiplierFootsteps { get; set; } = 1f;

        [DataMember(Name = "loudness_multiplier_headset", EmitDefaultValue = false, IsRequired = true)]
        public float LoudnessMultiplierHeadset { get; set; } = 1.3f;

        [DataMember(Name = "loudness_multiplier_helmet_low_deaf", EmitDefaultValue = false, IsRequired = true)]
        public float LoudnessMultiplierHelmetLowDeaf { get; set; } = 0.8f;

        [DataMember(Name = "loudness_multiplier_helmet_high_deaf", EmitDefaultValue = false, IsRequired = true)]
        public float LoudnessMultiplierHelmetHighDeaf { get; set; } = 0.6f;

        [DataMember(Name = "suspicious_time", EmitDefaultValue = false, IsRequired = true)]
        public MinMaxConfig SuspiciousTime { get; set; } = new MinMaxConfig();

        [DataMember(Name = "max_suspicious_time", EmitDefaultValue = false, IsRequired = true)]
        public Dictionary<string, int> MaxSuspiciousTime { get; set; } = new Dictionary<string, int>();

        [DataMember(Name = "suspicion_cooldown_time", EmitDefaultValue = false, IsRequired = true)]
        public float SuspicionCooldownTime { get; set; } = 30;

        public HearingSensorConfig()
        {

        }

        [OnDeserializing]
        void OnDeserializing(StreamingContext ctx)
        {

        }
    }
}
