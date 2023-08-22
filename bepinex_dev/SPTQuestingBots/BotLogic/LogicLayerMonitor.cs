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
        private BotOwner botOwner = null;
        private string layerName = null;
        private AICoreLayerClass<BotLogicDecision> layer = null;
        private Stopwatch canUseTimer = Stopwatch.StartNew();

        public bool IsActive
        {
            get { return layer?.IsActive == true; }
        }

        public void Init(BotOwner _botOwner, string _layerName)
        {
            botOwner = _botOwner;
            layerName = _layerName;
        }

        private void Update()
        {
            if ((botOwner == null) || (layerName == null))
            {
                return;
            }

            if (layer == null)
            {
                layer = BotBrains.GetBrainLayerForBot(botOwner, layerName);
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
            bool shallUse = IsLayerRequested();

            if (shallUse && (canUseTimer.ElapsedMilliseconds / 1000f > minTimeFromLastUse))
            {
                return true;
            }

            return false;
        }

        public void RestartUseTimer()
        {
            canUseTimer.Restart();
        }
    }
}
