using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace SPTQuestingBots.Controllers
{
    public static class BotObjectiveManagerFactory
    {
        private static Dictionary<BotOwner, Components.BotObjectiveManager> botObjectiveManagers = new Dictionary<BotOwner, Components.BotObjectiveManager>();

        public static void Clear()
        {
            botObjectiveManagers.Clear();
        }

        public static Components.BotObjectiveManager GetObjectiveManager(this BotOwner botOwner)
        {
            if (botObjectiveManagers.TryGetValue(botOwner, out var objectiveManager))
            {
                return objectiveManager;
            }

            return null;
        }

        public static Components.BotObjectiveManager GetOrAddObjectiveManager(this BotOwner botOwner)
        {
            Components.BotObjectiveManager objectiveManager = GetObjectiveManager(botOwner);
            if (objectiveManager != null)
            {
                return objectiveManager;
            }

            objectiveManager = botOwner.GetPlayer.gameObject.GetOrAddComponent<Components.BotObjectiveManager>();
            objectiveManager.Init(botOwner);

            botObjectiveManagers[botOwner] = objectiveManager;

            return objectiveManager;
        }
    }
}
