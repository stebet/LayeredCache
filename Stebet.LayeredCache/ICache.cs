// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System.Threading.Tasks;

namespace Stebet.LayeredCache
{
    public interface ICache
    {
        Task AddAsync<T>(string key, CacheItem<T> item);
        Task RemoveAsync(string key);
        Task ClearAsync();
        Task<CacheItem<T>> GetAsync<T>(string key);
    }
}
