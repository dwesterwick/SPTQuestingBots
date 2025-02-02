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

namespace SPTQuestingBots.Patches
{
    internal class ServerRequestPatch : ModulePatch
    {
        public static int ForcePScavCount { get; set; } = 0;

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

            int pScavChance;

            if (ConfigController.Config.BotSpawns.Enabled && ConfigController.Config.BotSpawns.PScavs.Enabled)
            {
                pScavChance = ForcePScavCount > 0 ? 100 : 0;
            }
            else if (ConfigController.Config.AdjustPScavChance.Enabled)
            {
                double[][] chanceVsTimeRemainingFraction = ConfigController.Config.AdjustPScavChance.ChanceVsTimeRemainingFraction;
                float remainingRaidTimeFraction = Helpers.RaidHelpers.GetRaidTimeRemainingFraction();

                pScavChance = (int)Math.Round(ConfigController.InterpolateForFirstCol(chanceVsTimeRemainingFraction, remainingRaidTimeFraction));
            }
            else
            {
                return;
            }

            Class19<List<WaveInfo>> originalParams = (Class19<List<WaveInfo>>)legacyParams.Params;
            legacyParams.Params = new ModifiedParams(originalParams.conditions, pScavChance);

            ForcePScavCount = Math.Max(0, ForcePScavCount - 1);
        }

        internal class ModifiedParams
        {
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("conditions")]
            public List<WaveInfo> Conditions { get; set; }

            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            [JsonProperty("PScavChance")]
            public float PScavChance { get; set; }

            public ModifiedParams()
            {

            }

            public ModifiedParams(List<WaveInfo> _conditions, float _PScavChance)
            {
                Conditions = _conditions;
                PScavChance = _PScavChance;
            }

            public override string ToString()
            {
                return JsonConvert.SerializeObject(this, Formatting.Indented);
            }

            public override int GetHashCode()
            {
                return 861877474 + EqualityComparer<List<WaveInfo>>.Default.GetHashCode(Conditions);
            }

            public override bool Equals(object value)
            {
                ModifiedParams modifiedParams = value as ModifiedParams;
                return modifiedParams != null && EqualityComparer<List<WaveInfo>>.Default.Equals(Conditions, modifiedParams.Conditions);
            }
        }
    }
}
