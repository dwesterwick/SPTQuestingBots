using EFT;
using QuestingBots.BotLogic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.BotLogic
{
    public class LogicLayerMonitor : MonoBehaviour
    {
        public string LayerName { get; private set; } = null;

        private BotOwner botOwner = null;
        private AICoreLayerClass<BotLogicDecision> layer = null;
        private Stopwatch canUseTimer = Stopwatch.StartNew();

        public bool IsActive
        {
            get { return layer?.IsActive == true; }
        }

        public void Init(BotOwner _botOwner, string _layerName)
        {
            botOwner = _botOwner;
            LayerName = _layerName;
        }

        private void Update()
        {
            if ((botOwner == null) || (LayerName == null))
            {
                return;
            }

            if (layer == null)
            {
                layer = BotBrains.GetBrainLayerForBot(botOwner, LayerName);
            }
        }

        public bool IsLayerRequested()
        {
            if (!IsActive)
            {
                return false;
            }

            return layer?.ShallUseNow() == true;
        }

        public bool CanUseLayer(float minTimeFromLastUse)
        {
            if (canUseTimer.ElapsedMilliseconds / 1000f < minTimeFromLastUse)
            {
                return false;
            }

           if (IsLayerRequested())
           {
                return true;
           }

            return false;
        }

        public void RestartCanUseTimer()
        {
            canUseTimer.Restart();
        }
    }
}
