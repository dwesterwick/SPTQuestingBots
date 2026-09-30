using EFT;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace QuestingBots.BotLogic.Recovery
{
    public class BotRecoveryAction : BehaviorExtensions.GoToPositionAbstractAction
    {
        public BotRecoveryAction(BotOwner _BotOwner) : base(_BotOwner, 20)
        {
            SetBaseAction(AIActionsList.CreateNode(BotLogicDecision.holdPosition, BotOwner));
        }

        public override void Start()
        {
            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        public override void Update(DrakiaXYZ.BigBrain.Brains.CustomLayer.ActionData data)
        {
            BotOwner.Sprint(false);
            BotOwner.StopMove();

            float targetPose = ObjectiveManager.CoverPointSelector.GetTargetPoseAtCoverPoint();
            BotOwner.SetPose(targetPose);

            Vector3 lookDirection = -1 * ObjectiveManager.CoverPointSelector.ToWallVector;
            BotOwner.Steering.LookToDirection(lookDirection);

            // Don't allow expensive parts of this behavior to run too often
            if (!canUpdate())
            {
                return;
            }

            BotOwner.BotLight.TurnOff(false, true);
            BotOwner.Memory.BotCurrentCoverInfo.TryCheckSafe();
            CheckRemainingAmmo();
        }

        private void CheckRemainingAmmo()
        {
            if (BotOwner.WeaponManager.UnderbarrelLauncherController.IsActive)
            {
                if (BotOwner.WeaponManager.UnderbarrelLauncherController.NeedToReload())
                {
                    BotOwner.WeaponManager.UnderbarrelLauncherController.TryReload(null);
                }

                return;
            }

            if (!BotOwner.WeaponManager.HaveBullets)
            {
                BotOwner.WeaponManager.Reload.TryReload();
            }
        }
    }
}
