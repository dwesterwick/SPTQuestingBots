using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Aki.Reflection.Patching;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using UnityEngine;
using UnityEngine.AI;
using StupidPathCalculatorClass = GClass423;

namespace SPTQuestingBots.Patches
{
    public class GoToPositionPatch : ModulePatch
    {
        private static FieldInfo botOwnerField = AccessTools.Field(typeof(StupidPathCalculatorClass), "botOwner_0");
        private static FieldInfo pathControllerField = AccessTools.Field(typeof(StupidPathCalculatorClass), "gclass422_0");

        protected override MethodBase GetTargetMethod()
        {
            return typeof(StupidPathCalculatorClass).GetMethod(
                "GoToPosition",
                BindingFlags.Public | BindingFlags.Instance,
                null,
                new Type[] { typeof(Vector3), typeof(bool), typeof(float), typeof(bool), typeof(bool), typeof(bool) },
                null);
        }

        [PatchPostfix]
        private static void PatchPostfix(StupidPathCalculatorClass __instance, ref NavMeshPathStatus __result, Vector3 target, bool slowAtTheEnd, float reachDist, bool getUpWithCheck, bool mustHaveWay, bool onlyShortTrie = false)
        {
            if (mustHaveWay || (__result == NavMeshPathStatus.PathComplete))
            {
                return;
            }

            BotOwner botOwner = (BotOwner)botOwnerField.GetValue(__instance);

            NavMeshPath navMeshPath = new NavMeshPath();
            if (NavMesh.CalculatePath(botOwner.Position, target, -1, navMeshPath) && navMeshPath.status != NavMeshPathStatus.PathInvalid)
            {
                if (navMeshPath.corners.Length == 0)
                {
                    LoggingController.LogWarning("Unity's calculated path has zero corners");
                    return;
                }

                PathControllerClass pathControllerClass = (PathControllerClass)pathControllerField.GetValue(__instance);
                pathControllerClass.GoToByWay(navMeshPath.corners, reachDist);

                __result = navMeshPath.status;
            }
        }
    }
}
