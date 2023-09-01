using EFT;
using EFT.HealthSystem;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic
{
    public class BotMonitor
    {
        private BotOwner botOwner;

        public BotMonitor(BotOwner _botOwner)
        {
            botOwner = _botOwner;
        }

        public bool ShouldSearchForEnemy(double maxTimeSinceCombatEnded)
        {
            bool hasCloseDanger = botOwner.Memory.DangerData.HaveCloseDanger;

            bool wasInCombat = (Time.time - botOwner.Memory.LastTimeHit) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.EnemySetTime) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.LastEnemyTimeSeen) < maxTimeSinceCombatEnded;
            wasInCombat |= (Time.time - botOwner.Memory.UnderFireTime) < maxTimeSinceCombatEnded;

            return wasInCombat || hasCloseDanger;
        }

        public bool ShouldWaitForFollowers()
        {
            IReadOnlyCollection<BotOwner> followers = BotQuestController.GetAliveFollowers(botOwner);
            if (followers.Count > 0)
            {
                IEnumerable<float> followerDistances = followers.Select(f => Vector3.Distance(botOwner.Position, f.Position));
                if
                (
                    followerDistances.Any(d => d > ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.Furthest)
                    || followerDistances.All(d => d > ConfigController.Config.BotQuestingRequirements.MaxFollowerDistance.Nearest)
                )
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsAbleBodied(bool writeToLog)
        {
            if (botOwner.Medecine.FirstAid.Have2Do || botOwner.Medecine.SurgicalKit.HaveWork)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to heal");
                }
                return false;
            }

            if (100f * botOwner.HealthController.Hydration.Current / botOwner.HealthController.Hydration.Maximum < ConfigController.Config.BotQuestingRequirements.MinHydration)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to drink");
                }
                return false;
            }

            if (100f * botOwner.HealthController.Energy.Current / botOwner.HealthController.Energy.Maximum < ConfigController.Config.BotQuestingRequirements.MinEnergy)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " needs to eat");
                }
                return false;
            }

            ValueStruct healthHead = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Head);
            ValueStruct healthChest = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Chest);
            ValueStruct healthStomach = botOwner.HealthController.GetBodyPartHealth(EBodyPart.Stomach);
            ValueStruct healthLeftLeg = botOwner.HealthController.GetBodyPartHealth(EBodyPart.LeftLeg);
            ValueStruct healthRightLeg = botOwner.HealthController.GetBodyPartHealth(EBodyPart.RightLeg);

            if
            (
                (100f * healthHead.Current / healthHead.Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthHead)
                || (100f * healthChest.Current / healthChest.Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthChest)
                || (100f * healthStomach.Current / healthStomach.Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthStomach)
                || (100f * healthLeftLeg.Current / healthLeftLeg.Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthLegs)
                || (100f * healthRightLeg.Current / healthRightLeg.Maximum < ConfigController.Config.BotQuestingRequirements.MinHealthLegs)
            )
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " cannot heal");
                }
                return false;
            }

            if (100f * botOwner.GetPlayer.Physical.Overweight > ConfigController.Config.BotQuestingRequirements.MaxOverweightPercentage)
            {
                if (writeToLog)
                {
                    LoggingController.LogInfo("Bot " + botOwner.Profile.Nickname + " is overweight");
                }
                return false;
            }

            return true;
        }
    }
}
