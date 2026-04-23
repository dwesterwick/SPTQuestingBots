using EFT.GameTriggers;
using QuestingBots.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace QuestingBots.Utils
{
    public class EftAccessToolsUtil
    {
        private AccessToolsLazyLoadField<TriggerZone, string> triggerZoneTriggerId;
        private AccessToolsLazyLoadField<HandlerTriggerState, string> handlerTriggerStateTriggerId;
        private AccessToolsLazyLoadField<HandlerEffect, string> handlerEffectTriggerId;

        private AccessToolsLazyLoadField<HandlerPlaySoundAdvanced, string> handlerPlaySoundAdvancedPlayTrigger;

        private AccessToolsLazyLoadField<HandlerTriggerState, string[]> handlerTriggerStateControlledTriggerId;

        private AccessToolsLazyLoadField<HandlerEffect, HandlerEffect.EffectsSet> handlerEffectEffects;

        private AccessToolsLazyLoadField<HandlerBotsEvent, string> botsEventId;
        
        public EftAccessToolsUtil()
        {
            triggerZoneTriggerId = new AccessToolsLazyLoadField<TriggerZone, string>("_triggerId", "");
            handlerTriggerStateTriggerId = new AccessToolsLazyLoadField<HandlerTriggerState, string>("_triggerId", "");
            handlerEffectTriggerId = new AccessToolsLazyLoadField<HandlerEffect, string>("_triggerId", "");

            handlerPlaySoundAdvancedPlayTrigger = new AccessToolsLazyLoadField<HandlerPlaySoundAdvanced, string>("_playTriggerId", "");

            handlerTriggerStateControlledTriggerId = new AccessToolsLazyLoadField<HandlerTriggerState, string[]>("_controlledTriggerId", Array.Empty<string>());

            handlerEffectEffects = new AccessToolsLazyLoadField<HandlerEffect, HandlerEffect.EffectsSet>("_effects", new HandlerEffect.EffectsSet());

            botsEventId = new AccessToolsLazyLoadField<HandlerBotsEvent, string>("_botsEventId", "");
        }

        public string GetTriggerId(TriggerZone triggerZone) => triggerZoneTriggerId.GetValue(triggerZone);
        public string GetTriggerId(HandlerTriggerState handlerTriggerState) => handlerTriggerStateTriggerId.GetValue(handlerTriggerState);
        public string GetTriggerId(HandlerEffect handlerEffect) => handlerEffectTriggerId.GetValue(handlerEffect);

        public string GetPlayTriggerId(HandlerPlaySoundAdvanced handlerPlaySoundAdvanced) => handlerPlaySoundAdvancedPlayTrigger.GetValue(handlerPlaySoundAdvanced);

        public string[] GetControlledTriggerId(HandlerTriggerState handlerTriggerState) => handlerTriggerStateControlledTriggerId.GetValue(handlerTriggerState);

        public HandlerEffect.EffectsSet GetEffects(HandlerEffect handlerEffect) => handlerEffectEffects.GetValue(handlerEffect);

        public string GetBotsEventId(HandlerBotsEvent handlerBotsEvent) => botsEventId.GetValue(handlerBotsEvent);
        public string GetBotsEventId(GameObject gameObject)
        {
            HandlerBotsEvent handlerBotsEvent = gameObject.GetComponent<HandlerBotsEvent>();
            return GetBotsEventId(handlerBotsEvent);
        }
    }
}
