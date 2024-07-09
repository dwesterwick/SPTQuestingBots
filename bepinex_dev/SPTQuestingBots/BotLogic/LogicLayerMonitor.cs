﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using DrakiaXYZ.BigBrain.Brains;
using EFT;
using HarmonyLib;
using SPTQuestingBots.Controllers;
using UnityEngine;

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

        public bool CanLayerBeUsed => layer?.IsActive == true;
        public double TimeSinceLastRequested => lastRequestedTimer.IsRunning ? lastRequestedTimer.ElapsedMilliseconds / 1000.0 : double.MaxValue;
        
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
                layer = GetBrainLayerForBot(botOwner, LayerName);
            }
        }

        public bool IsLayerRequested()
        {
            // Check if the layer can be used by the bot (but not necessarily is in use)
            if (!CanLayerBeUsed)
            {
                return false;
            }

            // Check if the layer should be used by the bot
            bool isRequested = layer?.ShallUseNow() == true;

            if (isRequested)
            {
                lastRequestedTimer.Restart();
            }

            return isRequested;

        }

        public bool CanUseLayer(float minTimeFromLastUse)
        {
            // Only use the layer if enough time has passed from the last time it was used
            if (canUseTimer.ElapsedMilliseconds / 1000f < minTimeFromLastUse)
            {
                return false;
            }

            // Check if the layer should be used by the bot
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

        public static ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> GetBrainLayersForBot(BotOwner botOwner)
        {
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> emptyCollection = new ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>>(new AICoreLayerClass<BotLogicDecision>[0]);

            // This happens sometimes, and I don't know why
            if (botOwner?.Brain?.BaseBrain == null)
            {
                LoggingController.LogError("Invalid base brain for bot " + botOwner.GetText());
                return emptyCollection;
            }

            // Find the field that stores the list of brain layers assigned to the bot
            Type AICoreStrategyAbstractClassType = typeof(AICoreStrategyAbstractClass<BotLogicDecision>);
            FieldInfo layerListField = AICoreStrategyAbstractClassType.GetField("list_0", BindingFlags.NonPublic | BindingFlags.Instance);
            if (layerListField == null)
            {
                LoggingController.LogError("Could not find brain layer list in type " + AICoreStrategyAbstractClassType.FullName);
                return emptyCollection;
            }

            // Get the list of brain layers for the bot
            List<AICoreLayerClass<BotLogicDecision>> layerList = (List<AICoreLayerClass<BotLogicDecision>>)layerListField.GetValue(botOwner.Brain.BaseBrain);
            if (layerList == null)
            {
                LoggingController.LogError("Could not retrieve brain layers for bot " + botOwner.GetText());
                return emptyCollection;
            }

            return new ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>>(layerList);
        }

        public static IEnumerable<string> GetBrainLayerNamesForBot(BotOwner botOwner)
        {
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> brainLayers = GetBrainLayersForBot(botOwner);
            return brainLayers.Select(l => l.Name());
        }

        public static AICoreLayerClass<BotLogicDecision> GetBrainLayerForBot(BotOwner botOwner, string layerName)
        {
            // Get all of the brain layers assigned to the bot
            ReadOnlyCollection<AICoreLayerClass<BotLogicDecision>> brainLayers = GetBrainLayersForBot(botOwner);

            // Try to find the matching layer
            IEnumerable<AICoreLayerClass<BotLogicDecision>> matchingLayers = brainLayers.Where(l => l.Name() == layerName);
            if (!matchingLayers.Any())
            {
                return null;
            }

            // Check if multiple layers with the same name exist in the list
            if (matchingLayers.Count() > 1)
            {
                LoggingController.LogWarning("Found multiple brain layers with the name \"" + layerName + "\". Returning the first match.");
            }

            return matchingLayers.First();
        }

        // This checks if the brain layer CAN be used, not if it's currently being used
        public static bool IsBrainLayerActiveForBot(BotOwner botOwner, string layerName)
        {
            AICoreLayerClass<BotLogicDecision> brainLayer = GetBrainLayerForBot(botOwner, layerName);
            if (brainLayer == null)
            {
                //LoggingController.LogWarning("Could not find brain layer with the name \"" + layerName + "\".");
                return false;
            }

            return brainLayer.IsActive;
        }

        public static CustomLayer GetExternalCustomLayer(AICoreLayerClass<BotLogicDecision> layer)
        {
            if (layer == null)
            {
                return null;
            }

            Assembly bigBrainAssembly = Assembly.GetAssembly(typeof(BrainManager));
            if (bigBrainAssembly == null)
            {
                LoggingController.LogError("Could get the BigBrain assembly");
                return null;
            }

            Type customLayerWrapperType = bigBrainAssembly.GetType("DrakiaXYZ.BigBrain.Internal.CustomLayerWrapper", false);
            if (customLayerWrapperType == null)
            {
                LoggingController.LogError("Could not find CustomLayerWrapper type");
                return null;
            }

            FieldInfo customLayerField = AccessTools.Field(customLayerWrapperType, "customLayer");
            if (customLayerField == null)
            {
                LoggingController.LogError("Could not find customLayer field");
                return null;
            }

            CustomLayer customLayer = (CustomLayer)customLayerField.GetValue(layer);
            if (layer == null)
            {
                LoggingController.LogError("Could not get CustomLayer");
                return null;
            }

            return customLayer;
        }
    }
}
