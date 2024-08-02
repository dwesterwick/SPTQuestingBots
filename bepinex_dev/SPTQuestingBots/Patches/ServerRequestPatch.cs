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

        protected override MethodBase GetTargetMethod()
        {
            string methodName = "CreateFromLegacyParams";

            Type targetType = FindTargetType(methodName);
            LoggingController.LogInfo("Found type for ServerRequestPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        private static void PatchPrefix(ref LegacyParamsStruct legacyParams)
        {
            string originalUrl = legacyParams.Url;

            string generateBotUrl = "/client/game/bot/generate";
            if (originalUrl.EndsWith(generateBotUrl))
            {
                int pScavChance;
                if (ConfigController.Config.BotSpawns.Enabled && ConfigController.Config.BotSpawns.PScavs.Enabled)
                {
                    if (ForcePScavCount > 0)
                    {
                        pScavChance = 100;
                    }
                    else
                    {
                        pScavChance = 0;
                    }
                }
                else if (ConfigController.Config.AdjustPScavChance.Enabled)
                {
                    pScavChance = (int)Math.Round(ConfigController.InterpolateForFirstCol(ConfigController.Config.AdjustPScavChance.ChanceVsTimeRemainingFraction, getRaidTimeRemainingFraction()));
                }
                else
                {
                    return;
                }

                Class17<List<WaveInfo>> originalParams = (Class17<List<WaveInfo>>)legacyParams.Params;
                legacyParams.Params = new ModifiedParams(originalParams.conditions, pScavChance);

                ForcePScavCount = Math.Max(0, ForcePScavCount - 1);
            }
        }

        public static Type FindTargetType(string methodName)
        {
            List<Type> targetTypeOptions = SPT.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.GetMethods().Any(m => m.Name.Contains(methodName)))
                .ToList();

            if (targetTypeOptions.Count != 1)
            {
                throw new TypeLoadException("Cannot find any type containing method " + methodName);
            }

            return targetTypeOptions[0];
        }

        private static float getRaidTimeRemainingFraction()
        {
            if (SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return SPT.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }

            return (float)SPT.SinglePlayer.Utils.InRaid.RaidChangesUtil.RaidTimeRemainingFraction;
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
