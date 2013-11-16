using System.Runtime.Caching;

namespace Stebet.LayeredCache.Tests
{
    public class RuntimeCache : ICache
    {
        private readonly MemoryCache cache;

        public RuntimeCache(string cacheName)
        {
            this.cache = new MemoryCache(cacheName);
        }

        public void Add<T>(string key, CacheItem<T> item)
        {
            cache.Add(key, item, item.ExpiresAt);
        }

        public void Remove(string key)
        {
            cache.Remove(key);
        }

        public void Clear()
        {
            cache.Trim(100);
        }

        public CacheItem<T> Get<T>(string key)
        {
            return cache.Get(key) as CacheItem<T>;
        }
    }
}
