namespace QuestingBots.Utils.Internal
{
    public class ObjectCache<T>
    {
        private T? cachedValue;

        public ObjectCache() { }

        public ObjectCache(T? value) : this()
        {
            CacheValue(value);
        }

        public void CacheValue(T? value)
        {
            cachedValue = Clone(value);
        }

        public void CacheValueAndThrowIfNull(T value)
        {
            CacheValue(value);

            if (cachedValue == null)
            {
                throw new NullReferenceException("Cached value was null");
            }
        }

        public T? GetValue()
        {
            return Clone(cachedValue);
        }

        public T GetValueAndThrowIfNull()
        {
            T? clone = GetValue();
            if (clone == null)
            {
                throw new NullReferenceException("Cached value was null");
            }

            return clone;
        }

        private T? Clone(T? value)
        {
            return FastCloner.FastCloner.DeepClone(value);
        }
    }
}
