using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace QuestingBots.Helpers
{
    public static class DatabaseHelpers
    {
        public static Location GetAndVerifyLocation(this DatabaseService databaseService, string locationId)
        {
            Location? location = databaseService.GetLocation(locationId);
            if (location == null)
            {
                throw new InvalidOperationException($"Cannot find location \"${locationId}\" in database.");
            }

            return location;
        }

        public static IEnumerable<Location> EnumerateLocations(this DatabaseService databaseService)
        {
            return databaseService.GetLocations().GetDictionary().Values;
        }

        public static IEnumerable<string> EnumerateLocationIDs(this DatabaseService databaseService)
        {
            return databaseService.GetLocations().GetDictionary().Keys;
        }

        public static bool IsFence(this Trader? trader) => trader?.Base?.Id == Traders.FENCE;

        public static IEnumerable<Trader> NotIncludingFence(this IEnumerable<Trader> traders) => traders.Where(t => !t.IsFence());

        public static IDictionary<TKey, Trader> NotIncludingFence<TKey>(this IDictionary<TKey, Trader> traders) where TKey : notnull
        {
            return traders.Where(t => !t.Value.IsFence()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool HasOffers(this Trader? trader) => trader?.Assort?.Items?.Count > 0;

        public static IEnumerable<Trader> WithOffers(this IEnumerable<Trader> traders) => traders.Where(t => t.HasOffers());

        public static IDictionary<TKey, Trader> WithOffers<TKey>(this IDictionary<TKey, Trader> traders) where TKey : notnull
        {
            return traders.Where(t => t.Value.HasOffers()).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public static bool Contains(this IEnumerable<string> ids, MongoId id) => ids.Any(id.Equals);
    }
}
