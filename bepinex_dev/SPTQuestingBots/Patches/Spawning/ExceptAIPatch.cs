using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPT.Reflection.Patching;
using SPTQuestingBots.Controllers;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.Patches.Spawning
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
            __result = persons.HumanAndSimulatedPlayers();

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
