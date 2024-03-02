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
    public class LoadBotsPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "LoadBots";
            string paramName = "conditions";

            Type targetType = FindTargetType(methodName, paramName).BaseType;
            LoggingController.LogInfo("Found type for LoadBotsPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPrefix]
        private static void PatchPrefix(List<WaveInfo> conditions)
        {
            LoggingController.LogInfo("Loading bots (Role: " + conditions[0].Role.ToString() + ", Waves: " + conditions.Count + ")...");

            float raidTimeRemainingFraction;
            if (Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.HasRaidStarted())
            {
                raidTimeRemainingFraction = Aki.SinglePlayer.Utils.InRaid.RaidTimeUtil.GetRaidTimeRemainingFraction();
            }
            else
            {
                raidTimeRemainingFraction = (float)Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.NewEscapeTimeMinutes / Aki.SinglePlayer.Utils.InRaid.RaidChangesUtil.OriginalEscapeTimeMinutes;
            }

            // TODO: This doesn't seem to work
            bool preventPScav = ConfigController.Config.AdjustPScavChance.DisableForGroups && (conditions.Count > 1);

            ConfigController.AdjustPScavChance(raidTimeRemainingFraction, preventPScav);
        }

        public static Type FindTargetType(string methodName, string paramName)
        {
            List<Type> targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.GetMethods().Any(m => m.Name.Contains(methodName) && m.GetParameters().Any(p => p.Name == paramName)))
                .ToList();

            if (targetTypeOptions.Count == 0)
            {
                throw new TypeLoadException("Cannot find any type containing method " + methodName);
            }

            foreach (Type targetTypeOption in targetTypeOptions.ToArray())
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

            return targetTypeOptions[0];
        }
    }
}
