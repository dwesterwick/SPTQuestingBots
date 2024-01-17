using Aki.Custom.Airdrops;
using Aki.Reflection.Patching;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.Patches
{
    public class LoadBotsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "LoadBots";
            string paramName = "conditions";

            List<Type> targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.GetMethods().Any(m => m.Name.Contains(methodName) && m.GetParameters().Any(p => p.Name == paramName)))
                .ToList();

            if (targetTypeOptions.Count == 0)
            {
                throw new TypeLoadException("Cannot find any type containing method " + methodName);
            }

            foreach(Type targetTypeOption in targetTypeOptions.ToArray())
            {
                if (targetTypeOptions.Any(t => t.IsSubclassOf(targetTypeOption)))
                {
                    targetTypeOptions.Remove(targetTypeOption);
                }
            }

            if (targetTypeOptions.Count > 1)
            {
                throw new TypeLoadException("Found " + targetTypeOptions.Count + " types containing method " + methodName + ": " + string.Join(", ", targetTypeOptions.Select(t => t.FullName)));
            }

            LoggingController.LogInfo("Found class for LoadBotsPatch: " + targetTypeOptions[0].FullName);

            return targetTypeOptions[0].GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(List<WaveInfo> conditions)
        {
            LoggingController.LogInfo("Loading bots (" + conditions.Count + ")...");

            float raidTimeRemainingFraction;
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                raidTimeRemainingFraction = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }
            else
            {
                raidTimeRemainingFraction = (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewEscapeTimeMinutes / Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeMinutes;
            }

            bool preventPScav = ConfigController.Config.AdjustPScavChance.DisableForGroups && (conditions.Count > 1);

            ConfigController.AdjustPScavChance(raidTimeRemainingFraction, preventPScav);
        }
    }
}
