// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stebet.LayeredCache.Tests
{
    public class RuntimeCache : ICache
    {
        private readonly Dictionary<string, object> _cache = new Dictionary<string, object>();

        public virtual Task AddAsync<T>(string key, CacheItem<T> item)
        {
            _cache.Add(key, item);
            return Task.CompletedTask;
        }

        public virtual Task RemoveAsync(string key) => Task.FromResult(_cache.Remove(key));

        public virtual Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }

        public virtual Task<CacheItem<T>> GetAsync<T>(string key)
        {
            if (_cache.ContainsKey(key))
            {
                var item = _cache[key] as CacheItem<T>;
                return Task.FromResult((item != null && item.IsExpired) ? null : item);
            }

            return Task.FromResult<CacheItem<T>>(null);
        }
    }
}
