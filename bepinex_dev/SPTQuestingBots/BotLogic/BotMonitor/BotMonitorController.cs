using EFT;
using SPTQuestingBots.BehaviorExtensions;
using SPTQuestingBots.BotLogic.BotMonitor.Monitors;
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
        private Dictionary<Type, AbstractBotMonitor> monitors = new Dictionary<Type, AbstractBotMonitor>();

        public BotMonitorController(BotOwner _botOwner)
        {
            botOwner = _botOwner;
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
        }

        public T GetMonitor<T>() where T : AbstractBotMonitor
        {
            Type type = typeof(T);
            if (monitors.TryGetValue(type, out AbstractBotMonitor monitor))
            {
                return monitor as T;
            }

            return null;
        }

        protected void Awake()  
        {
            addSensors();

            monitors.Values.ExecuteForEach(monitor => monitor.Awake());
        }

        protected void Update() => monitors.Values.ExecuteForEach(monitor => monitor.Update());
        protected void OnDestroy() => monitors.Values.ExecuteForEach(monitor => monitor.OnDestroy());
    }
}
