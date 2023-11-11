using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SPTQuestingBots.Configuration
{
    public class ModConfig
    {
        [JsonProperty("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonProperty("debug")]
        public DebugConfig Debug { get; set; } = new DebugConfig();

        [JsonProperty("max_calc_time_per_frame_ms")]
        public float MaxCalcTimePerFrame { get; set; } = 5;

        [JsonProperty("questing")]
        public QuestingConfig Questing { get; set; } = new QuestingConfig();

        [JsonProperty("initial_PMC_spawns")]
        public InitialPMCSpawnsConfig InitialPMCSpawns { get; set; } = new InitialPMCSpawnsConfig();

        public ModConfig()
        {

        }
    }
}
