using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using EFT;

namespace SPTQuestingBots.BotLogic
{
    public class LogicLayerMonitor : MonoBehaviour
    {
        public string LayerName { get; private set; } = null;

        private BotOwner botOwner = null;
        private AICoreLayerClass<BotLogicDecision> layer = null;
        private Stopwatch maxLayerSearchTimer = new Stopwatch();
        private Stopwatch canUseTimer = Stopwatch.StartNew();
        private Stopwatch lastRequestedTimer = new Stopwatch();
        private float maxLayerSearchTime = 300;

        public bool CanLayerBeUsed
        {
            get { return layer?.IsActive == true; }
        }

        public double TimeSinceLastRequested
        {
            get { return lastRequestedTimer.IsRunning ? lastRequestedTimer.ElapsedMilliseconds / 1000.0 : double.MaxValue; }
        }

        public void Init(BotOwner _botOwner, string _layerName)
        {
            botOwner = _botOwner;
            LayerName = _layerName;

            maxLayerSearchTimer.Start();
        }

        public void Init(BotOwner _botOwner, string _layerName, float _maxLayerSearchTime)
        {
            maxLayerSearchTime = _maxLayerSearchTime * 1000;

            Init(_botOwner, _layerName);
        }

        private void Update()
        {
            if ((botOwner == null) || (LayerName == null))
            {
                return;
            }

            if ((layer == null) && (maxLayerSearchTimer.ElapsedMilliseconds < maxLayerSearchTime))
            {
                layer = BotBrains.GetBrainLayerForBot(botOwner, LayerName);
            }
        }

        public bool IsLayerRequested()
        {
            if (!CanLayerBeUsed)
            {
                return false;
            }

            bool isRequested = layer?.ShallUseNow() == true;

            if (isRequested)
            {
                lastRequestedTimer.Restart();
            }

            return isRequested;

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

        public string GetActiveLogicReason()
        {
            if (!CanLayerBeUsed)
            {
                return "";
            }

            AICoreActionResultStruct<BotLogicDecision> lastDecision = layer.GetDecision();
            if (lastDecision.Reason == null)
            {
                return "";
            }

            return lastDecision.Reason;
        }
    }
}
