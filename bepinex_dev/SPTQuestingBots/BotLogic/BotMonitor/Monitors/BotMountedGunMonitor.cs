using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPTQuestingBots.BotLogic.BotMonitor.Monitors
{
    public class BotMountedGunMonitor : AbstractBotMonitor
    {
        public bool WantsToUseStationaryWeapon { get; private set; } = false;

        private LogicLayerMonitor stationaryWSLayerMonitor;

        public BotMountedGunMonitor(BotOwner _botOwner) : base(_botOwner) { }

        public override void Start()
        {
            stationaryWSLayerMonitor = BotOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            stationaryWSLayerMonitor.Init(BotOwner, "StationaryWS");
        }

        public override void Update()
        {
            WantsToUseStationaryWeapon = wantsToUseStationaryWeapon();
        }

        private bool wantsToUseStationaryWeapon()
        {
            if (stationaryWSLayerMonitor.CanLayerBeUsed && stationaryWSLayerMonitor.IsLayerRequested())
            {
                return true;
            }

            return false;
        }
    }
}
