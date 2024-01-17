using Comfort.Common;
using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SPTQuestingBots.Controllers.Bots.Spawning
{
    public abstract class BotGenerator : MonoBehaviour
    {
        protected void SpawnBots(Models.BotSpawnInfo botSpawnInfo, Vector3[] positions)
        {
            BotSpawner botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;

            BotZone closestBotZone = botSpawnerClass.GetClosestZone(positions[0], out float dist);
            foreach (Vector3 position in positions)
            {
                botSpawnInfo.Data.AddPosition(position);
            }

            // In SPT-AKI 3.7.1, this is GClass732
            IBotCreator ibotCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            GroupActionsWrapper groupActionsWrapper = new GroupActionsWrapper(botSpawnerClass, botSpawnInfo);
            Func<BotOwner, BotZone, BotsGroup> getGroupFunction = new Func<BotOwner, BotZone, BotsGroup>(groupActionsWrapper.GetGroupAndSetEnemies);
            Action<BotOwner> callback = new Action<BotOwner>(groupActionsWrapper.CreateBotCallback);

            ibotCreator.ActivateBot(botSpawnInfo.Data, closestBotZone, false, getGroupFunction, callback, botSpawnerClass.GetCancelToken());
        }

        internal class GroupActionsWrapper
        {
            private BotsGroup group = null;
            private BotSpawner botSpawnerClass = null;
            private Models.BotSpawnInfo botSpawnInfo = null;
            private Stopwatch stopWatch = new Stopwatch();

            public GroupActionsWrapper(BotSpawner _botSpawnerClass, Models.BotSpawnInfo _botGroup)
            {
                botSpawnerClass = _botSpawnerClass;
                botSpawnInfo = _botGroup;
            }

            public BotsGroup GetGroupAndSetEnemies(BotOwner bot, BotZone zone)
            {
                if (group == null)
                {
                    group = botSpawnerClass.GetGroupAndSetEnemies(bot, zone);
                    group.Lock();
                }

                return group;
            }

            public void CreateBotCallback(BotOwner bot)
            {
                // I have no idea why BSG passes a stopwatch into this call...
                stopWatch.Start();

                MethodInfo method = AccessTools.Method(typeof(BotSpawner), "method_10");
                method.Invoke(botSpawnerClass, new object[] { bot, botSpawnInfo.Data, null, false, stopWatch });

                if (botSpawnInfo.ShouldBotBeBoss(bot))
                {
                    bot.Boss.SetBoss(botSpawnInfo.Count);
                }

                LoggingController.LogInfo("Bot " + bot.GetText() + " spawned in " + botSpawnInfo.SpawnType + " group #" + botSpawnInfo.GroupNumber);
                botSpawnInfo.Owners.Add(bot);
                botSpawnInfo.HasSpawned = true;
            }
        }
    }
}
