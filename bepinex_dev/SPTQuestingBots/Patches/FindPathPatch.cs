using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Patches
{
    public class FindPathPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            string methodName = "FindPath";

            Type targetType = FindTargetType(methodName);
            LoggingController.LogInfo("Found type for FindPathPatch: " + targetType.FullName);

            return targetType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        }

        [PatchPrefix]
        private static bool PatchPrefix(ref bool __result, Vector3 f, Vector3 t, out Vector3[] corners)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            if (NavMesh.CalculatePath(f, t, -1, navMeshPath) && navMeshPath.status != NavMeshPathStatus.PathInvalid)
            {
                corners = navMeshPath.corners;
                __result = true;
            }
            else
            {
                corners = null;
                __result = false;
            }

            return false;
        }

        public static Type FindTargetType(string methodName)
        {
            List<Type> targetTypeOptions = Aki.Reflection.Utils.PatchConstants.EftTypes
                .Where(t => t.GetMethods().Any(m => m.Name.Equals(methodName)))
                .ToList();

            if (targetTypeOptions.Count != 1)
            {
                throw new TypeLoadException("Cannot find any type containing method " + methodName);
            }

            return targetTypeOptions[0];
        }
    }
}
