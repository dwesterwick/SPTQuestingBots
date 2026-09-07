using Comfort.Common;
using EFT;
using QuestingBots.Helpers;
using QuestingBots.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace QuestingBots.BotLogic.ExternalMods.Functions.Loot
{
    public class InternalLootFunction : AbstractLootFunction
    {
        public override string MonitoredLayerName => "Looting";

        public InternalLootFunction(BotOwner _botOwner) : base(_botOwner)
        {
            
        }

        public override bool IsSearchingForLoot()
        {
            return false;
        }

        public override bool IsLooting()
        {
            if (BotOwner.ItemTaker.HaveItemToTake())
            {
                return true;
            }

            if (BotOwner.DeadBodyWork.ShallUse)
            {
                return true;
            }

            if (!BotOwner.InLootClusterRadius())
            {
                return false;
            }

            return BotOwner.PatrollingData.LootData.ClusterLootingNow();
        }

        public override bool TryPreventBotFromLooting(float duration)
        {
            BotOwner.PatrollingData.LootData.SetPauseFor(duration);

            return true;
        }

        public override bool TryForceBotToScanLoot()
        {
            //LogLootClusterDistance();

            BotOwner.PatrollingData.LootData.SetPauseFor(0);

            if (!BotOwner.TrySetNewTargetLootCluster())
            {
                return false;
            }

            //LogLootClusterDistance();

            return true;
        }

        private void LogLootClusterDistance()
        {
            Vector3? targetLootClusterPosition = BotOwner.PatrollingData.LootData.TargetLootCluster?.CenterPosition;
            if (targetLootClusterPosition != null)
            {
                float distanceToLootCluster = Vector3.Distance(BotOwner.Position, targetLootClusterPosition.Value);
                Singleton<LoggingUtil>.Instance.LogDebug(BotOwner.GetText() + " will loot around " + targetLootClusterPosition.Value + " (" + distanceToLootCluster + "m away)");
            }
        }
    }
}
