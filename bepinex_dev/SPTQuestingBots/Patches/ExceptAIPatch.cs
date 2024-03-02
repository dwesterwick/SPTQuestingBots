using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using SPTQuestingBots.Components.Spawning;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Patches
{
    public class ExceptAIPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "ExceptAI";

            Type targetType = FindTargetType(methodName);
            LoggingController.LogInfo("Found type for ExceptAIPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref IEnumerable<IPlayer>  __result, IEnumerable<IPlayer> persons)
        {
            //LoggingController.LogInfo("All Players: " + string.Join(", ", persons.Select(x => x.Profile.Nickname)));

            string[] generatedBotIDs = BotGenerator.GetAllGeneratedBotProfileIDs().ToArray();
            __result = persons.Where(p => !p.IsAI || generatedBotIDs.Contains(p.Profile.Id));

            //LoggingController.LogInfo("Non-AI Players: " + string.Join(", ", __result.Select(x => x.Profile.Nickname)));

            return false;
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
    }
}
