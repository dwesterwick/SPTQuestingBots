using SPTarkov.Server.Core.Models.Spt.Config;

namespace QuestingBots.Helpers
{
    public static class CloningHelpers
    {
        public static Dictionary<TKey, TValue> Clone<TKey, TValue>(this Dictionary<TKey, TValue> original) where TKey : notnull
        {
            Dictionary<TKey, TValue> clone = new Dictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> kvp in original)
            {
                clone.Add(kvp.Key, kvp.Value);
            }
            return clone;
        }

        public static ScavRaidTimeLocationSettings Clone(this ScavRaidTimeLocationSettings original)
        {
            ScavRaidTimeLocationSettings clone = new ScavRaidTimeLocationSettings();
            clone.ReducedChancePercent = original.ReducedChancePercent;
            clone.AdjustWaves = original.AdjustWaves;
            clone.MinDynamicLootPercent = original.MinDynamicLootPercent;
            clone.MinStaticLootPercent = original.MinStaticLootPercent;
            clone.ReduceLootByPercent = original.ReduceLootByPercent;
            clone.ReductionPercentWeights = original.ReductionPercentWeights.Clone();

            return clone;
        }
    }
}
