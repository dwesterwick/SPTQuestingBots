using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using EFT;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning.Advanced
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
        protected static bool PatchPrefix(ref IEnumerable<IPlayer>  __result, IEnumerable<IPlayer> persons)
        {
            //LoggingController.LogInfo("All Players: " + string.Join(", ", persons.Select(x => x.Profile.Nickname)));

            __result = persons.Where(p => p.ShouldPlayerBeTreatedAsHuman());

            //LoggingController.LogInfo("Non-AI Players: " + string.Join(", ", __result.Select(x => x.Profile.Nickname)));

            return false;
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
    }
}
