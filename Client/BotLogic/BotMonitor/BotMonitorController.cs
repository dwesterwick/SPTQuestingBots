using EFT;
using QuestingBots.BehaviorExtensions;
using QuestingBots.BotLogic.BotMonitor.Monitors;
using QuestingBots.Components;
using QuestingBots.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuestingBots.BotLogic.BotMonitor
{
    public class BotMonitorController : MonoBehaviourDelayedUpdate
    {
        private BotOwner botOwner = null!;
        private bool _initComplete = false;
        private Dictionary<Type, AbstractBotMonitor> monitors = new Dictionary<Type, AbstractBotMonitor>();
        private BotQuestingDecisionMonitor questingDecisionMonitor = null!;

        public BotQuestingDecision CurrentDecision => questingDecisionMonitor?.CurrentDecision ?? BotQuestingDecision.None;
        public bool HasAQuestingBoss => questingDecisionMonitor?.HasAQuestingBoss ?? false;

        public static BotMonitorController GetBotMonitorController(BotOwner botOwner)
        {
            BotMonitorController botMonitorController = botOwner.gameObject.GetOrAddComponent<BotLogic.BotMonitor.BotMonitorController>();
            botMonitorController.Init(botOwner);

            return botMonitorController;
        }

        public void Init(BotOwner _botOwner)
        {
            botOwner = _botOwner;

            addSensors();
        }

        private void addSensors()
        {
            if (_initComplete)
            {
                return;
            }

            monitors.Add(typeof(BotHearingMonitor), new BotHearingMonitor(botOwner));
            monitors.Add(typeof(BotMountedGunMonitor), new BotMountedGunMonitor(botOwner));
            monitors.Add(typeof(BotExtractMonitor), new BotExtractMonitor(botOwner));
            monitors.Add(typeof(BotLootingMonitor), new BotLootingMonitor(botOwner));
            monitors.Add(typeof(BotCombatMonitor), new BotCombatMonitor(botOwner));
            monitors.Add(typeof(BotHealthMonitor), new BotHealthMonitor(botOwner));
            monitors.Add(typeof(BotQuestingMonitor), new BotQuestingMonitor(botOwner));

            questingDecisionMonitor = new BotQuestingDecisionMonitor(botOwner);
            monitors.Add(typeof(BotQuestingDecisionMonitor), questingDecisionMonitor);

            _initComplete = true;
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

            BotObjectiveManager? objectiveManager = botOwner.GetObjectiveManager();
            if (objectiveManager == null)
            {
                return;
            }

            if ((botOwner.BotState != EBotState.Active) || botOwner.IsDead)
            {
                return;
            }

            monitors.Values.ExecuteForEach(monitor => monitor.UpdateAlways());

            if (!objectiveManager.IsQuestingAllowed)
            {
                questingDecisionMonitor.ForceDecision(BotQuestingDecision.None);
                return;
            }

            monitors.Values.ExecuteForEach(monitor => monitor.UpdateIfQuesting());
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
                T? monitor = monitors[monitorType] as T;
                return monitor!;
            }

            return null!;
        }
    }
}
