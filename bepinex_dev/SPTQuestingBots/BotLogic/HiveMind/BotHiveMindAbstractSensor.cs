using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.HiveMind
{
    public abstract class BotHiveMindAbstractSensor
    {
        protected Dictionary<BotOwner, bool> botState = new Dictionary<BotOwner, bool>();
        protected bool defaultValue = false;

        public BotHiveMindAbstractSensor()
        {

        }

        public BotHiveMindAbstractSensor(bool _defaultValue)
        {
            defaultValue = _defaultValue;
        }

        public virtual void RegisterBot(BotOwner bot)
        {
            if (bot == null)
            {
                throw new ArgumentNullException("Cannot register a null bot", nameof(bot));
            }

            if (!botState.ContainsKey(bot))
            {
                botState.Add(bot, false);
            }
        }

        public bool IsBotRegistered(BotOwner bot)
        {
            if (bot == null)
            {
                return false;
            }

            return botState.ContainsKey(bot);
        }

        public virtual void Update(Action<BotOwner> additionalAction = null)
        {
            foreach (BotOwner bot in botState.Keys.ToArray())
            {
                // Need to check if the reference is for a null object, meaning the bot was despawned and disposed
                if (bot == null)
                {
                    continue;
                }

                if (!bot.isActiveAndEnabled || bot.IsDead)
                {
                    botState[bot] = defaultValue;
                }

                if (additionalAction != null)
                {
                    additionalAction(bot);
                }
            }
        }

        public virtual void UpdateForBot(BotOwner bot, bool value)
        {
            updateDictionaryValue(botState, bot, value);
        }

        public virtual bool CheckForBot(BotOwner bot)
        {
            return botState.ContainsKey(bot) && botState[bot];
        }

        public virtual bool CheckForBossOfBot(BotOwner bot)
        {
            return checkBotState(botState, BotHiveMindMonitor.GetBoss(bot)) ?? defaultValue;
        }

        public virtual bool CheckForFollowers(BotOwner bot)
        {
            return checkStateForAnyFollowers(botState, bot);
        }

        public virtual bool CheckForGroup(BotOwner bot)
        {
            return checkStateForAnyGroupMembers(botState, bot);
        }

        // NOTE: It didn't make sense to use generics for the rest of the class, but we'll keep it here just in case
        private void updateDictionaryValue<T>(Dictionary<BotOwner, T> dict, BotOwner bot, T value)
        {
            if (bot == null)
            {
                return;
            }

            if (dict.ContainsKey(bot))
            {
                dict[bot] = value;
            }
            else
            {
                dict.Add(bot, value);
            }
        }

        private bool? checkBotState(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            if (dict.TryGetValue(bot, out bool value))
            {
                return value;
            }

            return null;
        }

        private bool checkStateForAnyFollowers(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            if (!BotHiveMindMonitor.botFollowers.ContainsKey(bot))
            {
                return false;
            }

            foreach (BotOwner follower in BotHiveMindMonitor.botFollowers[bot].ToArray())
            {
                if (!dict.TryGetValue(follower, out bool value))
                {
                    continue;
                }

                if (value)
                {
                    return true;
                }
            }

            return false;
        }

        private bool checkStateForAnyGroupMembers(Dictionary<BotOwner, bool> dict, BotOwner bot)
        {
            BotOwner boss = BotHiveMindMonitor.GetBoss(bot) ?? bot;

            if (checkBotState(dict, boss) == true)
            {
                return true;
            }

            if (checkStateForAnyFollowers(dict, boss))
            {
                return true;
            }

            return false;
        }
    }
}
