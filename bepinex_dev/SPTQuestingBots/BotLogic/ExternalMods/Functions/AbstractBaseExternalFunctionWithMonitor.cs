using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;
using SPTQuestingBots.Helpers;

namespace SPTQuestingBots.BotLogic.ExternalMods.Functions
{
    public abstract class AbstractBaseExternalFunctionWithMonitor : AbstractBaseExternalFunction
    {
        public abstract string MonitoredLayerName { get; }

        private LogicLayerMonitor layerMonitor;

        public bool CanMonitoredLayerBeUsed => layerMonitor.CanLayerBeUsed;

        public AbstractBaseExternalFunctionWithMonitor(BotOwner _botOwner) : base(_botOwner)
        {
            layerMonitor = _botOwner.GetPlayer.gameObject.AddComponent<LogicLayerMonitor>();
            layerMonitor.Init(_botOwner, MonitoredLayerName);
        }

        public bool CanUseMonitoredLayer(float minTimeFromLastUse) => layerMonitor.CanUseLayer(minTimeFromLastUse);
        public void ResetMonitoredLayerCanUseTimer() => layerMonitor.RestartCanUseTimer();

        public bool IsMonitoredLayerActive()
        {
            if (!CanMonitoredLayerBeUsed)
            {
                return false;
            }

            string layerName = BotOwner.GetActiveLayerTypeName() ?? "null";
            if (layerName.Contains(layerMonitor.LayerName) || layerMonitor.IsLayerRequested())
            {
                return true;
            }

            return false;
        }
    }
}
