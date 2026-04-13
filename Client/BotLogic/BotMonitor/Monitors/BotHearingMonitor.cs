using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.ExternalMods;
using QuestingBots.BotLogic.ExternalMods.Functions.Hearing;
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
using UnityEngine;

namespace QuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotHearingMonitor : AbstractBotMonitor
    {
        public bool IsSuspicious { get; private set; } = false;

        private bool soundPlayedEventAdded = false;
        private float lastEnemySoundHeardTime = 0;
        private AbstractHearingFunction hearingFunction = null!;
        private double suspiciousTime = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Min;
        private float maxSuspiciousTime = 60;
        private float nextTimeSuspicionAllowed = 0;
        private Stopwatch totalSuspiciousTimer = new Stopwatch();
        private Stopwatch notSuspiciousTimer = Stopwatch.StartNew();

        public BotHearingMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            hearingFunction = ExternalModHandler.CreateHearingFunction(BotOwner);

            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.Enabled)
            {
                return;
            }

            Singleton<BotEventHandler>.Instance.OnSoundPlayed += enemySoundHeard;
            soundPlayedEventAdded = true;

            BotOwner.GetPlayer.OnIPlayerDeadOrUnspawn += (player) => { removeSoundPlayedEvent(); };

            updateMaxSuspiciousTime();
        }

        public override void UpdateIfQuesting()
        {
            IsSuspicious = isSuspicious();
        }

        public override void OnDestroy()
        {
            removeSoundPlayedEvent();
        }

        private void removeSoundPlayedEvent()
        {
            if (!soundPlayedEventAdded)
            {
                return;
            }

            Singleton<BotEventHandler>.Instance.OnSoundPlayed -= enemySoundHeard;
            soundPlayedEventAdded = false;
        }

        public bool TrySetIgnoreHearing(float duration, bool value)
        {
            bool hearingIgored = hearingFunction.TryIgnoreHearing(value, false, duration);
            if (hearingIgored && value)
            {
                nextTimeSuspicionAllowed = Time.time + duration;
            }
            else
            {
                nextTimeSuspicionAllowed = 0;
            }

            return hearingIgored;
        }

        private bool isSuspicious()
        {
            bool wasSuspiciousTooLong = totalSuspiciousTimer.ElapsedMilliseconds / 1000 > maxSuspiciousTime;
            //if (wasSuspiciousTooLong && totalSuspiciousTimer.IsRunning)
            //{
            //    Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " has been suspicious for too long");
            //}

            if (!wasSuspiciousTooLong && BotMonitor.GetMonitor<BotHearingMonitor>().shouldBeSuspicious(suspiciousTime))
            {
                if (!BotHiveMindMonitor.GetValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner))
                {
                    suspiciousTime = BotMonitor.GetMonitor<BotHearingMonitor>().updateSuspiciousTime();
                    //Singleton<LoggingUtil>.Instance.LogInfo("Bot " + BotOwner.GetText() + " will be suspicious for " + suspiciousTime + " seconds");

                    BotMonitor.GetMonitor<BotLootingMonitor>().TryPreventBotFromLooting((float)suspiciousTime);
                }

                totalSuspiciousTimer.Start();
                notSuspiciousTimer.Reset();

                BotMonitor.GetMonitor<BotHealthMonitor>().PauseHealthMonitoring();

                BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner, true);
                return true;
            }

            if (notSuspiciousTimer.ElapsedMilliseconds / 1000 > Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspicionCooldownTime)
            {
                //if (wasSuspiciousTooLong)
                //{
                //    Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " is now allowed to be suspicious");
                //}

                totalSuspiciousTimer.Reset();
            }
            else
            {
                totalSuspiciousTimer.Stop();
            }

            notSuspiciousTimer.Start();

            BotMonitor.GetMonitor<BotHealthMonitor>().ResumeHealthMonitoring();

            BotHiveMindMonitor.UpdateValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner, false);
            return false;
        }

        private bool shouldBeSuspicious(double maxTimeSinceDangerSensed)
        {
            bool shouldBeSuspicious = (Time.time - lastEnemySoundHeardTime) < maxTimeSinceDangerSensed;
            return shouldBeSuspicious;
        }

        private int updateSuspiciousTime()
        {
            System.Random random = new System.Random();
            int min = (int)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Min;
            int max = (int)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Max;

            return random.Next(min, max);
        }

        private void updateMaxSuspiciousTime()
        {
            string locationId = Singleton<GameWorld>.Instance.GetComponent<Components.LocationData>().CurrentLocation.Id;

            if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime.ContainsKey(locationId))
            {
                maxSuspiciousTime = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime[locationId];
            }
            else if (Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime.ContainsKey("default"))
            {
                maxSuspiciousTime = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxSuspiciousTime["default"];
            }
            else
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not set max suspicious time for " + BotOwner.GetText() + ". Defaulting to 60s.");
            }
        }

        private void enemySoundHeard(IPlayer iplayer, Vector3 position, float power, AISoundType type)
        {
            // Ignore dead or despawned bots
            if ((iplayer == null) || !iplayer.HealthController.IsAlive)
            {
                return;
            }

            // Ignore noises the bot makes itself
            if (iplayer.ProfileId == BotOwner.ProfileId)
            {
                return;
            }

            // Ignore noises that aren't from enemy bots or you
            if (!BotOwner.EnemiesController.EnemyInfos.Any(e => e.Key.ProfileId == iplayer.ProfileId))
            {
                return;
            }

            // Adjust the sound power based on the bot's loadout and the type of noise
            float adjustedPower = power * BotOwner.HearingMultiplier();
            adjustedPower *= (type == AISoundType.step) ? Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.LoudnessMultiplierFootsteps : 1;
            if (adjustedPower < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MinCorrectedSoundPower)
            {
                //Singleton<LoggingUtil>.Instance.LogInfo("Power: " + power + ", Adjusted Power: " + adjustedPower);
                return;
            }

            // Ignore sounds that the bot cannot hear
            float hearingRange = BotOwner.Settings.Current.CurrentHearingSense * adjustedPower;
            float dist = Vector3.Distance(BotOwner.Position, position);
            if (dist > hearingRange)
            {
                return;
            }

            if (shouldIgnoreSound(type, dist))
            {
                return;
            }

            //Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " heard " + type.ToString() + " " + dist + "m away from " + iplayer.GetText());

            lastEnemySoundHeardTime = Time.time;
        }

        private bool shouldIgnoreSound(AISoundType soundType, float distance)
        {
            if (Time.time < nextTimeSuspicionAllowed)
            {
                return false;
            }

            switch (soundType)
            {
                case AISoundType.step:
                    if (distance < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxDistanceFootsteps)
                    {
                        return false;
                    }
                    break;
                case AISoundType.gun:
                    if (distance < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxDistanceGunfire)
                    {
                        return false;
                    }
                    break;
                case AISoundType.silencedGun:
                    if (distance < Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.MaxDistanceGunfireSuppressed)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }
    }
}
