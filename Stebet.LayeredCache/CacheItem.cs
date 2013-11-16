using System;

namespace Stebet.LayeredCache
{
    public class CacheItem<T>
    {
        public T Item { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public CacheItem(T item, DateTime expiresAt)
        {
            this.Item = item;
            this.ExpiresAt = expiresAt;
        }
    }
}