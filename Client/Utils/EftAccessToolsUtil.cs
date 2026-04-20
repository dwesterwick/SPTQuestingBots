using EFT.GameTriggers;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace QuestingBots.Utils
{
    public class EftAccessToolsUtil
    {
        private FieldInfo triggerZoneTriggerIdFieldInfo;
        private FieldInfo handlerTriggerStateTriggerIdFieldInfo;
        private FieldInfo handlerTriggerStateControlledTriggerIdFieldInfo;
        private FieldInfo handlerEffectTriggerIdFieldInfo;
        private FieldInfo handlerEffectEffectsFieldInfo;
        private FieldInfo botsEventIdFieldInfo;
        private FieldInfo handlerPlaySoundAdvancedPlayTriggerIdFieldInfo;

        public EftAccessToolsUtil()
        {
            triggerZoneTriggerIdFieldInfo = AccessTools.Field(typeof(TriggerZone), "_triggerId");
            handlerTriggerStateTriggerIdFieldInfo = AccessTools.Field(typeof(HandlerTriggerState), "_triggerId");
            handlerTriggerStateControlledTriggerIdFieldInfo = AccessTools.Field(typeof(HandlerTriggerState), "_controlledTriggerId");
            handlerEffectTriggerIdFieldInfo = AccessTools.Field(typeof(HandlerEffect), "_triggerId");
            handlerEffectEffectsFieldInfo = AccessTools.Field(typeof(HandlerEffect), "_effects");
            botsEventIdFieldInfo = AccessTools.Field(typeof(HandlerBotsEvent), "_botsEventId");
            handlerPlaySoundAdvancedPlayTriggerIdFieldInfo = AccessTools.Field(typeof(HandlerPlaySoundAdvanced), "_playTriggerId");
        }

        public string GetTriggerId(TriggerZone triggerZone)
        {
            if (triggerZone == null)
            {
                return string.Empty;
            }

            string? triggerId = triggerZoneTriggerIdFieldInfo.GetValue(triggerZone) as string;
            return triggerId ?? string.Empty;
        }

        public string GetTriggerId(HandlerTriggerState handlerTriggerState)
        {
            if (handlerTriggerState == null)
            {
                return string.Empty;
            }

            string? triggerId = handlerTriggerStateTriggerIdFieldInfo.GetValue(handlerTriggerState) as string;
            return triggerId ?? string.Empty;
        }

        public string GetTriggerId(HandlerEffect handlerEffect)
        {
            if (handlerEffect == null)
            {
                return string.Empty;
            }

            string? triggerId = handlerEffectTriggerIdFieldInfo.GetValue(handlerEffect) as string;
            return triggerId ?? string.Empty;
        }

        public string[] GetControlledTriggerId(HandlerTriggerState handlerTriggerState)
        {
            if (handlerTriggerState == null)
            {
                return Array.Empty<string>();
            }

            string[]? controlledTriggerId = handlerTriggerStateControlledTriggerIdFieldInfo.GetValue(handlerTriggerState) as string[];
            return controlledTriggerId ?? Array.Empty<string>();
        }

        public HandlerEffect.EffectsSet GetEffects(HandlerEffect handlerEffect)
        {
            if (handlerEffect == null)
            {
                return new HandlerEffect.EffectsSet();
            }

            HandlerEffect.EffectsSet effects = (HandlerEffect.EffectsSet)handlerEffectEffectsFieldInfo.GetValue(handlerEffect);
            return effects;
        }

        public string GetBotsEventId(HandlerBotsEvent handlerBotsEvent)
        {
            if (handlerBotsEvent == null)
            {
                return string.Empty;
            }

            string? botsEventId = botsEventIdFieldInfo.GetValue(handlerBotsEvent) as string;
            return botsEventId ?? string.Empty;
        }

        public string GetBotsEventId(GameObject gameObject)
        {
            HandlerBotsEvent handlerBotsEvent = gameObject.GetComponent<HandlerBotsEvent>();
            return GetBotsEventId(handlerBotsEvent);
        }

        public string GetPlayTriggerId(HandlerPlaySoundAdvanced handlerPlaySoundAdvanced)
        {
            if (handlerPlaySoundAdvanced == null)
            {
                return string.Empty;
            }

            string? playTriggerId = handlerPlaySoundAdvancedPlayTriggerIdFieldInfo.GetValue(handlerPlaySoundAdvanced) as string;
            return playTriggerId ?? string.Empty;
        }
    }
}
