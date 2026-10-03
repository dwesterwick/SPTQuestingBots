using System;
using System.Collections.Generic;
using System.Text;

namespace QuestingBots.Helpers
{
    public static class LinqNonAllocHelpers
    {
        public static bool AnyNonAlloc<TObj>(this IEnumerable<TObj> objects, Predicate<TObj>? predicate = null)
        {
            foreach (TObj obj in objects)
            {
                if ((predicate == null) || predicate(obj))
                {
                    return true;
                }
            }

            return false;
        }

        public static int CountNonAlloc<TObj>(this IEnumerable<TObj> objects, Predicate<TObj>? predicate = null)
        {
            int count = 0;
            foreach (TObj obj in objects)
            {
                if ((predicate == null) || predicate(obj))
                {
                    count++;
                }
            }

            return count;
        }

        public static IEnumerable<TObj> WhereNonAlloc<TObj>(this IEnumerable<TObj> objects, Predicate<TObj> predicate)
        {
            foreach (TObj obj in objects)
            {
                if (predicate(obj))
                {
                    yield return obj;
                }
            }
        }

        public static IEnumerable<TOut> SelectNonAlloc<TObj, TOut>(this IEnumerable<TObj> objects, Func<TObj, TOut> predicate)
        {
            foreach (TObj obj in objects)
            {
                yield return predicate(obj);
            }
        }
    }
}
