using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using QuestingBots.Helpers;
using EFT;

namespace QuestingBots.Patches
{
    internal class ServerRequestPatch : ModulePatch
    {
        private static readonly string botGenerationEndpoint = "/client/game/bot/generate";

        protected override MethodBase GetTargetMethod()
        {
            return typeof(BackendRequestParams).GetMethod(nameof(BackendRequestParams.CreateFromLegacyParams), BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static void PatchPrefix(ref SendRequest legacyParams)
        {
            if (!legacyParams.Url.EndsWith(botGenerationEndpoint))
            {
                return;
            }

            BotGenerateRequestParams<List<CountTypeBotWave>> originalParams = (BotGenerateRequestParams<List<CountTypeBotWave>>)legacyParams.Params;
            AddPScavFlagsToWaves(originalParams.conditions, RaidHelpers.ShouldSpawnPScavByChance());
        }

        private static void AddPScavFlagsToWaves(List<CountTypeBotWave> waves, bool generatePScav)
        {
            for (int i = 0; i < waves.Count; i++)
            {
                CountTypeBotWave originalWave = waves[i];
                waves[i] = new WaveInfoWithPScavFlag(
                    originalWave,
                    generatePScav && (originalWave.Role == WildSpawnType.assault || originalWave.Role == WildSpawnType.assaultGroup)
                );
            }
        }

        internal class WaveInfoWithPScavFlag : CountTypeBotWave
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("GeneratePScav")]
            public bool GeneratePScav;

            public WaveInfoWithPScavFlag(CountTypeBotWave original, bool generatePScav = false) : base(original.Limit, original.Role, original.Difficulty)
            {
                GeneratePScav = generatePScav;
            }
        }
    }
}
