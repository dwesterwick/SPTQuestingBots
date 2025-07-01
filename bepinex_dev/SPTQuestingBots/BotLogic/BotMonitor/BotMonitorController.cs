using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
using SPTQuestingBots.Components;
using SPTQuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.BotMonitor
{
    public class BotMonitorController : MonoBehaviourDelayedUpdate
    {
        private BotOwner botOwner;
        private BotObjectiveManager objectiveManager;
        private Dictionary<Type, AbstractBotMonitor> monitors = new Dictionary<Type, AbstractBotMonitor>();
        private BotQuestingDecisionMonitor questingDecisionMonitor = null;

        public BotQuestingDecision CurrentDecision => questingDecisionMonitor?.CurrentDecision ?? BotQuestingDecision.None;

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;
            objectiveManager = botOwner.GetOrAddObjectiveManager();

            addSensors();
        }

        private void addSensors()
        {
            monitors.Add(typeof(BotHearingMonitor), new BotHearingMonitor(botOwner));
            monitors.Add(typeof(BotMountedGunMonitor), new BotMountedGunMonitor(botOwner));
            monitors.Add(typeof(BotExtractMonitor), new BotExtractMonitor(botOwner));
            monitors.Add(typeof(BotLootingMonitor), new BotLootingMonitor(botOwner));
            monitors.Add(typeof(BotCombatMonitor), new BotCombatMonitor(botOwner));
            monitors.Add(typeof(BotHealthMonitor), new BotHealthMonitor(botOwner));
            monitors.Add(typeof(BotQuestingMonitor), new BotQuestingMonitor(botOwner));

            questingDecisionMonitor = new BotQuestingDecisionMonitor(botOwner);
            monitors.Add(typeof(BotQuestingDecisionMonitor), questingDecisionMonitor);
        }

        protected void Start()
        {
            monitors.Values.ExecuteForEach(monitor => monitor.Start());
        }

        protected void Update()
        {
            if (!canUpdate())
            {
                return;
            }

            if ((botOwner.BotState != EBotState.Active) || botOwner.IsDead)
            {
                return;
            }

            if (!objectiveManager.IsQuestingAllowed)
            {
                questingDecisionMonitor.ForceDecision(BotQuestingDecision.None);
                return;
            }

            monitors.Values.ExecuteForEach(monitor => monitor.Update());
        }

        protected void OnDestroy()
        {
            monitors.Values.ExecuteForEach(monitor => monitor.OnDestroy());
        }

        public T GetMonitor<T>() where T : AbstractBotMonitor
        {
            Type monitorType = typeof(T);

            if (monitors.ContainsKey(monitorType))
            {
                return monitors[monitorType] as T;
            }

            return null;
        }
    }
}
