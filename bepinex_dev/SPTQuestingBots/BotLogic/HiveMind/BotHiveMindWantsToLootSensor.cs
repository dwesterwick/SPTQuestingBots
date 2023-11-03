using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.BotLogic.HiveMind
{
    public class BotHiveMindWantsToLootSensor : BotHiveMindAbstractSensor
    {
        private static Dictionary<BotOwner, DateTime> botLastLootingTime = new Dictionary<BotOwner, DateTime>();

        public BotHiveMindWantsToLootSensor() : base(false)
        {

        }

        public override void RegisterBot(BotOwner bot)
        {
            base.RegisterBot(bot);

            if (!botLastLootingTime.ContainsKey(bot))
            {
                botLastLootingTime.Add(bot, DateTime.MinValue);
            }
        }

        public override void UpdateForBot(BotOwner bot, bool wantsToLoot)
        {
            base.UpdateForBot(bot, wantsToLoot);

            if (wantsToLoot && (bot != null))
            {
                botLastLootingTime[bot] = DateTime.Now;
            }
        }

        public DateTime GetLastLootingTimeForBoss(BotOwner bot)
        {
            if ((bot == null) || !BotHiveMindMonitor.botBosses.ContainsKey(bot) || (BotHiveMindMonitor.botBosses[bot] == null))
            {
                return DateTime.MinValue;
            }

            return botLastLootingTime[bot];
        }
    }
}
