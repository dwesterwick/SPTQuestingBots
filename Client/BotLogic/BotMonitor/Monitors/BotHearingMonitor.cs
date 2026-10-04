using Comfort.Common;
using EFT;
using QuestingBots.BotLogic.HiveMind;
using QuestingBots.Components;
using QuestingBots.Configuration;
using QuestingBots.ExternalMods;
using QuestingBots.ExternalMods.Functions.Hearing;
using QuestingBots.Helpers;
using QuestingBots.Models;
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
        public bool WillInvestigateSounds { get; private set; } = true;

        private System.Random random = new System.Random();
        private bool soundPlayedEventAdded = false;
        private BotHeardSoundData? lastSoundData = null;
        private AbstractHearingFunction hearingFunction = null!;
        private double suspiciousTime = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Min;
        private float maxSuspiciousTime = 60;
        private float nextTimeSuspicionAllowed = 0;
        private Stopwatch totalSuspiciousTimer = new Stopwatch();
        private Stopwatch notSuspiciousTimer = Stopwatch.StartNew();

        public bool SuspicionAllowedByTime => Time.time >= nextTimeSuspicionAllowed;
        public Vector3? LastEstimatedSoundPosition => lastSoundData?.EstimatedPosition;

        public BotHearingMonitor(BotOwner _botOwner) : base(_botOwner)
        {
            if (random.Next(1, 100) <= Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.ChanceToInvestigateSounds)
            {
                WillInvestigateSounds = false;
            }
        }

        public override void Start()
        {
            hearingFunction = ExternalModHandler.CreateHearingFunction(BotOwner);

            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.Enabled)
            {
                return;
            }

            Singleton<GlobalEventDispatcher>.Instance.OnSoundPlayed += enemySoundHeard;
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

        public void IgnoreMostRecentSound()
        {
            lastSoundData = null;
        }

        private void removeSoundPlayedEvent()
        {
            if (!soundPlayedEventAdded)
            {
                return;
            }

            Singleton<GlobalEventDispatcher>.Instance.OnSoundPlayed -= enemySoundHeard;
            soundPlayedEventAdded = false;
        }

        public bool TrySetIgnoreHearing(float duration, bool value, bool ignoreUnderHire)
        {
            bool hearingIgnored = hearingFunction.TryIgnoreHearing(value, ignoreUnderHire, duration);
            if (hearingIgnored && value)
            {
                nextTimeSuspicionAllowed = Time.time + duration;
            }
            else
            {
                nextTimeSuspicionAllowed = 0;
            }

            return hearingIgnored;
        }

        private bool isSuspicious()
        {
            if (BotMonitor == null)
            {
                return false;
            }

            bool wasSuspiciousTooLong = totalSuspiciousTimer.ElapsedMilliseconds / 1000 > maxSuspiciousTime;
            //if (wasSuspiciousTooLong && totalSuspiciousTimer.IsRunning)
            //{
            //    Singleton<LoggingUtil>.Instance.LogInfo(BotOwner.GetText() + " has been suspicious for too long");
            //}

            if (!wasSuspiciousTooLong && shouldBeSuspicious(suspiciousTime))
            {
                if (!BotHiveMindMonitor.GetValueForBot(BotHiveMindSensorType.IsSuspicious, BotOwner))
                {
                    suspiciousTime = updateSuspiciousTime();
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
            if (lastSoundData == null)
            {
                return false;
            }

            bool shouldBeSuspicious = (Time.time - lastSoundData.Time) < maxTimeSinceDangerSensed;
            if (!shouldBeSuspicious)
            {
                lastSoundData = null;
            }

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
            float botHearingRange = BotOwner.Settings.Current.CurrentHearingSense * adjustedPower;
            float distanceToSound = Vector3.Distance(BotOwner.Position, position);
            float loudness = botHearingRange - distanceToSound;
            if (loudness < 0)
            {
                return;
            }

            if (shouldIgnoreSound(type, distanceToSound))
            {
                return;
            }

            //Singleton<LoggingUtil>.Instance.LogDebug("Bot " + BotOwner.GetText() + " heard " + type.ToString() + " " + dist + "m away from " + iplayer.GetText());

            // Don't pay attention to another bot unless it's making more noise
            float loudnessThresholdToChangeTarget = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.LoudnessThresholdToChangeTarget;
            if ((lastSoundData != null) && (iplayer != lastSoundData.EnemyPlayer) && (loudness < lastSoundData.Loundness * loudnessThresholdToChangeTarget))
            {
                return;
            }

            int minSuspiciousTime = (int)Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.SuspiciousTime.Min;
            if ((lastSoundData != null) && (lastSoundData.Time + minSuspiciousTime < Time.time) && (loudness < lastSoundData.Loundness / loudnessThresholdToChangeTarget))
            {
                return;
            }

            Vector3? estimatedPosition = estimateSoundPosition(position, loudness, botHearingRange, out float positionError);
            if (estimatedPosition == null)
            {
                lastSoundData = new BotHeardSoundData(Time.time, loudness, iplayer, lastSoundData?.EstimatedPosition, lastSoundData?.PositionError);

                //Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " heard " + iplayer.GetText() + " but could not identify where");
                return;
            }

            lastSoundData = new BotHeardSoundData(Time.time, loudness, iplayer, estimatedPosition.Value, positionError);
            //Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " heard " + iplayer.GetText() + " " + distanceToSound + "m away (loudness=" + loudness + ", error=" + positionError + "m)");
        }

        private Vector3? estimateSoundPosition(Vector3 actualPosition, float loudness, float botHearingRange, out float positionError)
        {
            MinMaxConfig estimatedSoundPostionError = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.EstimatedSoundPositionError;
            float loudnessGain = Singleton<ConfigUtil>.Instance.CurrentConfig.Questing.BotQuestingRequirements.HearingSensor.LoudnessGain;
            double error = Math.Max(0, 1.0 - (loudness * loudnessGain / botHearingRange)) * estimatedSoundPostionError.Max;

            float lastPositionError = lastSoundData?.PositionError ?? float.MaxValue;
            double maxError = Math.Min(lastPositionError, error);

            positionError = Math.Max((float)maxError, UnityEngine.Random.Range(0, (float)estimatedSoundPostionError.Min));

            Vector3 randomOffset = UnityEngine.Random.insideUnitSphere * positionError;
            Vector3 testPosition = actualPosition + randomOffset;

            float navMeshSearchRadius = (float)Math.Max(0.5, positionError);
            Vector3? estimatedPosition = Singleton<GameWorld>.Instance.GetComponent<LocationData>().FindNearestNavMeshPosition(testPosition, navMeshSearchRadius);

            return estimatedPosition;
        }

        private bool shouldIgnoreSound(AISoundType soundType, float distance)
        {
            if (!SuspicionAllowedByTime)
            {
                return true;
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
