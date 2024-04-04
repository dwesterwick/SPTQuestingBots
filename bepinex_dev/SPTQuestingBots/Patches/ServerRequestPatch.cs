using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
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

                legacyParams.Url = originalUrl.Replace(generateBotUrl, "/QuestingBots/GenerateBot/" + pScavChance);
                ForcePScavCount = Math.Max(0, ForcePScavCount - 1);
            }
        }

        public static Type FindTargetType(string methodName)
        {
            List<Type> targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes
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
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                return Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }

            return (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.RaidTimeRemainingFraction;
        }
    }
}
