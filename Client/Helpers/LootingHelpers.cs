using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class LootingHelpers
    {
        public static bool TrySetNewTargetLootCluster(this BotOwner botOwner)
        {
            AILootPointsCluster? lootPointsCluster = botOwner.FindTargetLootCluster();
            if (lootPointsCluster == null)
            {
                return false;
            }

            // From LootPatrolLayer.GetDecision()
            botOwner.PatrollingData.LootData.SetTargetLootCluster(lootPointsCluster);
            if (botOwner.PatrollingData.LootData.TargetLootCluster != null)
            {
                botOwner.PatrollingData.LootData.TargetLootCluster.ReserveForGroup(botOwner.BotsGroup);
                return true;
            }

            return false;
        }

        // From LootPatrolLayer.method_16()
        public static AILootPointsCluster? FindTargetLootCluster(this BotOwner botOwner)
        {
            List<AILootPointsCluster> lootPointClusters = Singleton<IBotGame>.Instance.BotsController.CoversData.Patrols.LootPointClusters;
            int num = -1;
            double num2 = 0.0;
            for (int i = 0; i < lootPointClusters.Count; i++)
            {
                AILootPointsCluster ailootPointsCluster = lootPointClusters[i];
                if (!botOwner.IsBadCluster(ailootPointsCluster) && botOwner.CheckCanLootCluster(ailootPointsCluster))
                {
                    float num3 = ailootPointsCluster.Evaluate(botOwner);
                    if ((double)num3 > num2)
                    {
                        num2 = (double)num3;
                        num = i;
                    }
                }
            }
            if (num < lootPointClusters.Count && num >= 0)
            {
                return lootPointClusters[num];
            }
            return null;
        }

        // From LootPatrolLayer
        public static bool CheckCanLootCluster(this BotOwner botOwner, AILootPointsCluster cluster)
        {
            return botOwner.Settings.FileSettings.Mind.CAN_LOOT_BOSS_CLUSTER || !cluster.CheckClusterInBossZone(botOwner.BotsGroup);
        }

        // From LootPatrolLayer
        public static bool IsBadCluster(this BotOwner botOwner, AILootPointsCluster cluster)
        {
            if (cluster.ConnectionGroup == botOwner.StartCorePoint.ConnectionGroupId && !cluster.Looted)
            {
                if (cluster.ReservedForGroup == null || cluster.ReservedForGroup == botOwner.BotsGroup)
                {
                    return false;
                }
            }
            return true;
        }

        // From LootPatrolLayer
        public static bool InLootClusterRadius(this BotOwner botOwner, float sqrRadiusMultiplier = 1f)
        {
            AILootPointsCluster targetLootCluster = botOwner.PatrollingData.LootData.TargetLootCluster;

            return targetLootCluster != null && botOwner.Position.SqrDistance(targetLootCluster.CenterPosition) < targetLootCluster.SquareRadius * sqrRadiusMultiplier;
        }
    }
}
