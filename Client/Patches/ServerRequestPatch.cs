using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using Comfort.Common;
using EFT;
using QuestingBots.Utils;

namespace QuestingBots.Patches
{
    internal class ServerRequestPatch : ModulePatch
    {
        private static readonly string botGenerationEndpoint = "/client/game/bot/generate";

        protected override MethodBase GetTargetMethod()
        {
            string methodName = "CreateFromLegacyParams";

            Type targetType = Helpers.TarkovTypeHelpers.FindTargetTypeByMethod(methodName);
            Singleton<LoggingUtil>.Instance.LogInfo("Found type for ServerRequestPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        protected static void PatchPrefix(ref LegacyParamsStruct legacyParams)
        {
            if (!legacyParams.Url.EndsWith(botGenerationEndpoint))
            {
                return;
            }

            Class19<List<WaveInfoClass>> originalParams = (Class19<List<WaveInfoClass>>)legacyParams.Params;
            AddPScavFlagsToWaves(originalParams.conditions, RaidHelpers.ShouldSpawnPScavByChance());
        }

        private static void AddPScavFlagsToWaves(List<WaveInfoClass> waves, bool generatePScav)
        {
            for (int i = 0; i < waves.Count; i++)
            {
                var originalWave = waves[i];
                waves[i] = new ExtendedWaveInfoClass(
                    originalWave,
                    generatePScav && (originalWave.Role == WildSpawnType.assault || originalWave.Role == WildSpawnType.assaultGroup)
                );
            }
        }

        internal class ExtendedWaveInfoClass : WaveInfoClass
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("GeneratePScav")]
            public bool GeneratePScav;

            public ExtendedWaveInfoClass(WaveInfoClass original, bool generatePScav) : base(original.Limit, original.Role, original.Difficulty)
            {
                GeneratePScav = generatePScav;
            }
        }
    }
}
