using Aki.Reflection.Patching;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static CW2.Animations.PhysicsSimulator.Val;

namespace SPTQuestingBots.Patches
{
    internal class ServerRequestPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "CreateFromLegacyParams";

            Type targetType = FindTargetType(methodName);
            LoggingController.LogInfo("Found type for ServerRequestPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        private static void PatchPrefix(ref GStruct21 legacyParams)
        {
            string oldUrl = legacyParams.Url;
            string generateBotUrl = "/client/game/bot/generate";

            if (oldUrl.EndsWith(generateBotUrl))
            {
                legacyParams.Url = oldUrl.Replace(generateBotUrl, "/QuestingBots/GenerateBot/100");
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
    }
}
