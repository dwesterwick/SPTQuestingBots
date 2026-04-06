using Comfort.Common;
using EFT;
using EFT.HealthSystem;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotHealthMonitor : AbstractBotMonitor
    {
        public bool IsMonitoringPaused { get; private set; } = false;
        public bool NeedsToHeal { get; private set; } = false;
        public bool NeedsToEatOrDrink { get; private set; } = false;
        public bool HasLowHealth { get; private set; } = false;
        public bool IsOverweight { get; private set; } = false;
        public bool IsAbleBodied { get; private set; } = false;

        private Stopwatch notAbleBodiedTimer = new Stopwatch();
        private Stopwatch mustHealTimer = new Stopwatch();

        public float NotAbleBodiedTime => notAbleBodiedTimer.ElapsedMilliseconds / 1000;
        public float NeedsToHealTime => mustHealTimer.ElapsedMilliseconds / 1000;

        public BotHealthMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void UpdateIfQuesting()
        {
            if (IsMonitoringPaused)
            {
                return;
            }

            NeedsToHeal = needsToHeal();
            NeedsToEatOrDrink = needsToEatOrDrink();
            HasLowHealth = hasLowHealth();
            IsOverweight = isOverweight();
            IsAbleBodied = isAbleBodied();

            if (BotOwner.BotsGroup.MembersCount > 1)
            {
                checkIfBotShouldSeparateFromGroup();
            }
        }

        private void checkIfBotShouldSeparateFromGroup()
        {
            if (NeedsToHealTime > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxNotAbleBodiedTime)
            {
                Singleton<LoggingUtil>.Instance.LogWarning("Waited " + NeedsToHealTime + "s for " + BotOwner.GetText() + " to heal");
                BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);
                return;
            }

            if (NotAbleBodiedTime > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.StuckBotDetection.MaxNotAbleBodiedTime)
            {
                List<string> reasons = new List<string>();
                if (NeedsToHeal)
                {
                    reasons.Add("unable to heal");
                }
                if (HasLowHealth)
                {
                    reasons.Add("low health");
                }
                if (NeedsToEatOrDrink)
                {
                    reasons.Add("hungry or thirsty");
                }
                if (IsOverweight)
                {
                    reasons.Add("overweight");
                }

                Singleton<LoggingUtil>.Instance.LogWarning("Waited " + NotAbleBodiedTime + "s for " + BotOwner.GetText() + " to be able-bodied due to: " + string.Join(", ", reasons));
                BotHiveMindMonitor.SeparateBotFromGroup(BotOwner);
                return;
            }
        }

        public void PauseHealthMonitoring()
        {
            notAbleBodiedTimer.Stop();
            mustHealTimer.Stop();
            IsMonitoringPaused = true;
        }

        public void ResumeHealthMonitoring()
        {
            IsMonitoringPaused = false;
        }

        private bool isAbleBodied()
        {
            if (NeedsToHeal || NeedsToEatOrDrink || HasLowHealth || IsOverweight)
            {
                if (IsAbleBodied)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is not able-bodied");
                }

                notAbleBodiedTimer.Start();
                return false;
            }

            if (!IsAbleBodied)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is now able-bodied");
            }

            notAbleBodiedTimer.Reset();
            return true;
        }

        private bool needsToHeal()
        {
            // Check if the bot needs to heal or perform surgery
            if (BotOwner.Medecine.FirstAid.Have2Do || BotOwner.Medecine.SurgicalKit.HaveWork)
            {
                if (!NeedsToHeal)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " needs to heal");
                }

                mustHealTimer.Start();
                return true;
            }

            if (NeedsToHeal)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " has finished healing");
            }

            mustHealTimer.Reset();
            return false;
        }

        private bool needsToEatOrDrink()
        {
            // Check if the bot needs to drink something
            if (100f * BotOwner.HealthController.Hydration.Current / BotOwner.HealthController.Hydration.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHydration)
            {
                if (!NeedsToEatOrDrink)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " needs to drink");
                }
                return true;
            }

            // Check if the bot needs to eat something
            if (100f * BotOwner.HealthController.Energy.Current / BotOwner.HealthController.Energy.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinEnergy)
            {
                if (!NeedsToEatOrDrink)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " needs to eat");
                }
                return true;
            }

            if (NeedsToEatOrDrink)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " no longer needs to eat or drink");
            }
            return false;
        }

        private bool hasLowHealth()
        {
            // Get the health of all of the bot's body parts
            ValueStruct healthHead = BotOwner.HealthController.GetBodyPartHealth(EBodyPart.Head);
            ValueStruct healthChest = BotOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest);
            ValueStruct healthStomach = BotOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach);
            ValueStruct healthLeftLeg = BotOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg);
            ValueStruct healthRightLeg = BotOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg);

            // Check if any of the bot's body parts need to be healed
            if
            (
                (100f * healthHead.Current / healthHead.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHealthHead)
                || (100f * healthChest.Current / healthChest.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHealthChest)
                || (100f * healthStomach.Current / healthStomach.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHealthStomach)
                || (100f * healthLeftLeg.Current / healthLeftLeg.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHealthLegs)
                || (100f * healthRightLeg.Current / healthRightLeg.Maximum < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MinHealthLegs)
            )
            {
                if (!HasLowHealth)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " has one or more body parts with health too low for questing");
                }
                return true;
            }

            if (HasLowHealth)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " now has enough health for questing");
            }
            return false;
        }

        private bool isOverweight()
        {
            // Check if the bot is too overweight
            if (100f * BotOwner.GetPlayer.Physical.Overweight > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.MaxOverweightPercentage)
            {
                if (!IsOverweight)
                {
                    Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is overweight");
                }
                return true;
            }

            if (IsOverweight)
            {
                Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " is no longer overweight");
            }
            return false;
        }
    }
}
