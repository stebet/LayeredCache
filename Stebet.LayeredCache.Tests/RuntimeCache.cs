// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System.Runtime.Caching;
using System.Threading.Tasks;

namespace Stebet.LayeredCache.Tests
{
    public class RuntimeCache : ICache
    {
        private readonly MemoryCache _cache;

        public RuntimeCache(string cacheName)
        {
            _cache = new MemoryCache(cacheName);
        }

        public virtual Task AddAsync<T>(string key, CacheItem<T> item) => Task.FromResult(_cache.Add(key, item, item.ExpiresAt));

        public virtual Task RemoveAsync(string key) => Task.FromResult(_cache.Remove(key));

        public virtual Task ClearAsync() => Task.FromResult(_cache.Trim(100));

        public virtual Task<CacheItem<T>> GetAsync<T>(string key)
        {
            var item = _cache.Get(key) as CacheItem<T>;
            return Task.FromResult((item != null && item.IsExpired) ? null : item);
        }
    }
}
