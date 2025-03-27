using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using SPT.Reflection.Patching;
using Comfort.Common;
using EFT;
using EFT.SynchronizableObjects;
using UnityEngine;
using UnityEngine.AI;

namespace SPTQuestingBots.Patches
{
    internal class AirdropLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when eairdropFallingStage_0=EAirdropFallingStage.Landed in ManualUpdate()
            return typeof(AirdropLogicClass).GetMethod("method_15", BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(AirdropSynchronizableObject ___airdropSynchronizableObject_0)
        {
            // Do not run this on Fika client machines
            if (!Helpers.RaidHelpers.IsHostRaid())
            {
                return;
            }

            AddNavMeshObstacle(___airdropSynchronizableObject_0);

            Vector3 airdropPosition = ___airdropSynchronizableObject_0.transform.position;
            Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().AddAirdropChaserQuest(airdropPosition);
        }

        private static void AddNavMeshObstacle(AirdropSynchronizableObject ___airdropSynchronizableObject_0)
        {
            NavMeshObstacle navMeshObstacle = ___airdropSynchronizableObject_0.gameObject.GetOrAddComponent<NavMeshObstacle>();
            navMeshObstacle.size = ___airdropSynchronizableObject_0.CollisionCollider.bounds.size;
            navMeshObstacle.carving = true;
        }
    }
}
