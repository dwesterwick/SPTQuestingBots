using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches
{
    internal class ServerRequestPatch : ModulePatch
    {
        private static readonly string botGenerationEndpoint = "/client/game/bot/generate";

        protected override MethodBase GetTargetMethod()
        {
            string methodName = "CreateFromLegacyParams";

            Type targetType = Helpers.TarkovTypeHelpers.FindTargetType(methodName);
            LoggingController.LogInfo("Found type for ServerRequestPatch: " + targetType.FullName);

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
            legacyParams.Params = new ModifiedParams(originalParams.conditions, RaidHelpers.ShouldSpawnPScavByChance());
        }

        internal class ModifiedParams
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("conditions")]
            public List<WaveInfoClass> Conditions { get; set; }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("GeneratePScav")]
            public bool GeneratePScav { get; set; }

            public ModifiedParams()
            {

            }

            public ModifiedParams(List<WaveInfoClass> _conditions, bool _GeneratePScav)
            {
                Conditions = _conditions;
                GeneratePScav = _GeneratePScav;
            }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }

            public override int GetHashCode()
            {
                return 861877474 + EqualityComparer<List<WaveInfoClass>>.Default.GetHashCode(Conditions);
            }

            public override bool Equals(object value)
            {
                ModifiedParams modifiedParams = value as ModifiedParams;
                return modifiedParams != null && EqualityComparer<List<WaveInfoClass>>.Default.Equals(Conditions, modifiedParams.Conditions);
            }
        }
    }
}
