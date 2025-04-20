using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Controllers;
using UnityEngine;

namespace SPTQuestingBots.Models
{
    public class BotSprintingController
    {
        public bool IsSprinting { get; private set; } = false;

        protected BotOwner BotOwner { get; private set; }

        private static Configuration.MinMaxConfig staminaLimits;
        private static float debounceTime;

        private float nextAllowedSprintTime = 0f;

        public BotSprintingController(BotOwner botOwner)
        {
            BotOwner = botOwner;

            staminaLimits = ConfigController.Config.Questing.SprintingLimitations.Stamina;
            debounceTime = ConfigController.Config.Questing.SprintingLimitations.EnableDebounceTime;
        }

        public void ExternalUpdate(bool value, bool withDebugCallback = true)
        {
            if (!trySprint(value, withDebugCallback))
            {
                //LoggingController.LogWarning("Blocked request to change sprinting for " + BotOwner.GetText() + " to " + value);
            }
        }

        public void ManualUpdate(bool value, bool withDebugCallback = true)
        {
            if (value && !BotOwner.Mover.NoSprint && BotOwner.GetPlayer.Physical.CanSprint && (BotOwner.GetPlayer.Physical.Stamina.NormalValue > staminaLimits.Max))
            {
                trySprint(true, withDebugCallback);
            }
            if (!value && BotOwner.Mover.NoSprint || !BotOwner.GetPlayer.Physical.CanSprint || (BotOwner.GetPlayer.Physical.Stamina.NormalValue < staminaLimits.Min))
            {
                trySprint(false, withDebugCallback);
            }
        }

        private bool trySprint(bool val, bool withDebugCallback = true)
        {
            if (val && (Time.time < nextAllowedSprintTime))
            {
                return IsSprinting;
            }

            // Previous method in SPT 3.10 and below
            //BotOwner.GetPlayer.EnableSprint(value);

            sprint(val, withDebugCallback);

            IsSprinting = val;
            nextAllowedSprintTime = Time.time + debounceTime;

            return true;
        }

        private void sprint(bool val, bool withDebugCallback = true)
        {
            // --- From BotOwner.Sprint ---
            if (val)
            {
                BotOwner.SetPose(1f);
                BotOwner.AimingManager.CurrentAiming.LoseTarget();
            }

            BotOwner.Mover.Sprint(val, withDebugCallback);
        }
    }
}
