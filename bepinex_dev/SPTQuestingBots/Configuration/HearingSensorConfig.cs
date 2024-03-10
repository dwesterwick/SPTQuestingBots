using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Configuration
{
    public class HearingSensorConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = false;

        [JsonProperty("min_corrected_sound_power")]
        public float MinCorrectedSoundPower { get; set; } = 17;

        [JsonProperty("max_distance_footsteps")]
        public float MaxDistanceFootsteps { get; set; } = 20;

        [JsonProperty("max_distance_gunfire")]
        public float MaxDistanceGunfire { get; set; } = 75;

        [JsonProperty("max_distance_gunfire_suppressed")]
        public float MaxDistanceGunfireSuppressed { get; set; } = 75;

        [JsonProperty("loudness_multiplier_footsteps")]
        public float LoudnessMultiplierFootsteps { get; set; } = 1f;

        [JsonProperty("loudness_multiplier_headset")]
        public float LoudnessMultiplierHeadset { get; set; } = 1.3f;

        [JsonProperty("loudness_multiplier_helmet_low_deaf")]
        public float LoudnessMultiplierHelmetLowDeaf { get; set; } = 0.8f;

        [JsonProperty("loudness_multiplier_helmet_high_deaf")]
        public float LoudnessMultiplierHelmetHighDeaf { get; set; } = 0.6f;

        [JsonProperty("suspicious_time")]
        public MinMaxConfig SuspiciousTime { get; set; } = new MinMaxConfig();

        [JsonProperty("max_suspicious_time")]
        public Dictionary<string, int> MaxSuspiciousTime { get; set; } = new Dictionary<string, int>();

        [JsonProperty("suspicion_cooldown_time")]
        public float SuspicionCooldownTime { get; set; } = 30;

        public HearingSensorConfig()
        {

        }
    }
}
