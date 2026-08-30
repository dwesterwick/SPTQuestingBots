using Comfort.Common;
using EFT;
using EFT.Airdrop;
using EFT.SynchronizableObjects;
using SPT.Reflection.Patching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace QuestingBots.Patches
{
    internal class AirdropLandPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // Called when eairdropFallingStage_0=EAirdropFallingStage.Landed in ManualUpdate()
            return typeof(ClientAirDrop).GetMethod(nameof(ClientAirDrop.CheckSurface), BindingFlags.Public | BindingFlags.Instance);
        }

        [PatchPostfix]
        protected static void PatchPostfix(AirdropSynchronizableObject ____syncObject)
        {
            // Do not run this on Fika client machines
            if (!Helpers.RaidHelpers.IsHostRaid())
            {
                return;
            }

            AddNavMeshObstacle(____syncObject);

            Vector3 airdropPosition = ____syncObject.transform.position;
            Bounds airdropBounds = ____syncObject.CollisionCollider.bounds;
            Singleton<GameWorld>.Instance.GetComponent<Components.BotQuestBuilder>().StartAddAirdropChaserQuest(airdropPosition, airdropBounds);
        }

        private static void AddNavMeshObstacle(AirdropSynchronizableObject ____syncObject)
        {
            NavMeshObstacle navMeshObstacle = ____syncObject.gameObject.GetOrAddComponent<NavMeshObstacle>();
            navMeshObstacle.size = ____syncObject.CollisionCollider.bounds.size;
            navMeshObstacle.carving = true;
        }
    }
}
