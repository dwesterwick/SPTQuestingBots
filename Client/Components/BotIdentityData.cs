using Comfort.Common;
using EFT;
using EFT.Ballistics;
using QuestingBots.Components.Spawning;
using QuestingBots.Controllers;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace QuestingBots.Components
{
    public class BotIdentityData : MonoBehaviour
    {
        private BotOwner _botOwner = null!;
        private bool _initComplete = false;

        public bool ActivationComplete { get; private set; } = false;
        public BotType BotType { get; private set; } = BotType.Undetermined;

        public static BotIdentityData GetBotIdentityData(BotOwner botOwner)
        {
            BotIdentityData botIdentityData = botOwner.gameObject.GetOrAddComponent<BotIdentityData>();
            botIdentityData.Init(botOwner);

            return botIdentityData;
        }

        public void Init(BotOwner botOwner)
        {
            if (_initComplete) return;

            _botOwner = botOwner;

            // Spread out the work to reduce the performance impact
            StartCoroutine(activateBot());

            _initComplete = true;
        }

        private IEnumerator activateBot()
        {
            registerBot();
            yield return null;

            registerBotComponents();
            yield return null;

            BotType = getBotType();
            yield return null;

            adjustEftBotCounts();
            yield return null;

            updateBotHostilities();
            yield return null;

            // Fix for bots getting stuck in Standby when enemy PMC's are near them
            _botOwner.StandBy.CanDoStandBy = false;

            ActivationComplete = true;
        }

        private void registerBot()
        {
            string roleName = _botOwner.Profile.Info.Settings.Role.ToString();
            Singleton<LoggingUtil>.Instance.LogInfo("Initial spawn type for bot " + _botOwner.GetText() + ": " + roleName);

            if (_botOwner.WillBeAPMC())
            {
                Controllers.BotRegistrationManager.RegisterPMC(_botOwner);
            }
            else if (_botOwner.WillBeABoss())
            {
                Controllers.BotRegistrationManager.RegisterBoss(_botOwner);
            }

            Controllers.BotRegistrationManager.WriteMessageForNewBotSpawn(_botOwner);

            if (_botOwner.IsARegisteredPMC() || _botOwner.WillBeAPlayerScav())
            {
                registerBotAsHumanPlayer();
            }
        }

        private void registerBotAsHumanPlayer()
        {
            if (!Singleton<ConfigUtil>.Instance.CurrentConfig.BotSpawns.Enabled)
            {
                return;
            }

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            botSpawnerClass.AddPlayer(_botOwner.GetPlayer());
            _botOwner.GetPlayer().OnPlayerDead += deletePlayer;
        }

        private static void deletePlayer(Player player, IPlayer lastAgressor, DamageInfo damage, EBodyPart part)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            try
            {
                botSpawnerClass.DeletePlayer(player.GetPlayer());
            }
            catch (Exception ex)
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not delete player " + player.GetText() + ": " + ex.Message);
                Singleton<LoggingUtil>.Instance.LogError(ex.StackTrace);
            }
        }

        private void registerBotComponents()
        {
            Singleton<GameWorld>.Instance.GetComponent<Components.DebugData>().RegisterBot(_botOwner);

            BotLogic.HiveMind.BotHiveMindMonitor.RegisterBot(_botOwner);
            if (!BotLogic.HiveMind.BotHiveMindMonitor.IsRegistered(_botOwner))
            {
                Singleton<LoggingUtil>.Instance.LogError("Could not register " + _botOwner.GetText() + " in BotHiveMindMonitor");
            }
        }

        private BotType getBotType()
        {
            return Controllers.BotRegistrationManager.GetBotType(_botOwner);
        }

        private void adjustEftBotCounts()
        {
            if (BotGenerator.GetAllGeneratedBotProfileIDs().Contains(_botOwner.Profile.Id))
            {
                reduceBotCounts();
            }
        }

        private void updateBotHostilities()
        {
            if (shouldMakeBotGroupHostileTowardAllBosses())
            {
                Controllers.BotRegistrationManager.MakeBotGroupHostileTowardAllBosses(_botOwner);
            }
        }

        private bool shouldMakeBotGroupHostileTowardAllBosses()
        {
            BotType botType = Controllers.BotRegistrationManager.GetBotType(_botOwner);

            float chance = Singleton<ConfigUtil>.Instance.CurrentConfig.ChanceOfBeingHostileTowardBosses.GetValue(botType) ?? 0;

            System.Random random = new System.Random();
            if (random.Next(1, 100) <= chance)
            {
                return true;
            }

            return false;
        }

        private void reduceBotCounts()
        {
            Singleton<LoggingUtil>.Instance.LogDebug("Adjusting EFT bot counts for " + _botOwner.GetText() + "...");

            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            if (_botOwner.Profile.Info.Settings.IsFollower())
            {
                botSpawnerClass._followersBotsCount--;
            }
            else if (_botOwner.Profile.Info.Settings.IsBoss())
            {
                botSpawnerClass._bossBotsCount--;
            }

            botSpawnerClass._allBotsCount--;
        }
    }
}
