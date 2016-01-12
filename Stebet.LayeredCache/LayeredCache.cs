// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Stebet.LayeredCache
{
    /// <summary>
    /// The LayeredCache class is a layered cache implementation. Cache implementations are checked in-order for the cached value and repopulated as required.
    /// </summary>
    public class LayeredCache
    {
        /// <summary>
        /// Stores the cache implementations.
        /// </summary>
        private readonly IList<ICache> _caches;

        /// <summary>
        /// Initializes a new instance of the LayeredCache class.
        /// </summary>
        /// <param name="caches">The ICache implementations to use for the cache.</param>
        public LayeredCache(params ICache[] caches)
        {
            if (caches.Length == 0)
            {
                throw new ArgumentException("The LayeredCache needs at least one ICache implementation.", nameof(caches));
            }

            _caches = caches.ToList();
        }

        /// <summary>
        /// Sets a value in the cache, overriding any existing value.
        /// </summary>
        /// <typeparam name="T">The type of the item to put in the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="item">The item to put in the cache.</param>
        /// <param name="expiresAt">The expiry date of the item to put in the cache.</param>
        public async Task SetAsync<T>(string key, T item, DateTime expiresAt)
        {
            var cacheItem = new CacheItem<T>(item, expiresAt);
            _caches.Select(async cache => await cache.AddAsync(key, cacheItem).ConfigureAwait(false));
        }

        /// <summary>
        /// Checks the caches for a value of type T and asynchronously populates them if they don't contain the value.
        /// </summary>
        /// <typeparam name="T">The type of the item to fetch from the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetchItemAsync">A method that asynchronously fetches the value if it is not found in the cache.</param>
        /// <param name="expiresAt">The expiry date of the item.</param>
        /// <returns>A Task containing the value of type T.</returns>
        public Task<T> GetAsync<T>(string key, Func<Task<T>> fetchItemAsync, DateTime expiresAt) => GetAsync(key, fetchItemAsync, x => expiresAt);

        /// <summary>
        /// Checks the caches for a value of type T and asynchronously populates them if they don't contain the value.
        /// </summary>
        /// <typeparam name="T">The type of the item to fetch from the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetchItemAsync">A method that asynchronously fetches the value if it is not found in the cache.</param>
        /// <param name="expiryEvaluator">A method that extracts the expiry date from the result.</param>
        /// <returns>A Task containing the value of type T.</returns>
        public async Task<T> GetAsync<T>(string key, Func<Task<T>> fetchItemAsync, Func<T, DateTime> expiryEvaluator)
        {
            Stack<ICache> cacheStack = null;
            CacheItem<T> result;

            foreach (ICache cache in _caches)
            {
                Debug.WriteLine($"Looking for item with key={key} in {cache.GetType()}.");
                result = await cache.GetAsync<T>(key).ConfigureAwait(false);

                if (result != null)
                {
                    Debug.WriteLine($"Found valid item with key={key} in {cache.GetType()}", key, cache.GetType());

                    // Let's populate the missing cache items
                    await RepopulateCacheWhereMissing(key, cacheStack, result).ConfigureAwait(false);

                    return result.Item;
                }

                if (cacheStack == null)
                {
                    cacheStack = new Stack<ICache>(_caches.Count);
                }

                // Expired or item not found, let's mark our cache for population and try the next cache (if available).
                Debug.WriteLine($"Didn't fint valid item with key={key} in {cache.GetType()}. Marking for population.");
                cacheStack.Push(cache);
            }

            // Still haven't found our item, let's get it from our "data store" and extract the expiryDate.
            T item = await fetchItemAsync().ConfigureAwait(false);
            DateTime expiresAt = expiryEvaluator(item);
            result = new CacheItem<T>(item, expiresAt);

            // Let's populate the missing cache items
            await RepopulateCacheWhereMissing(key, cacheStack, result).ConfigureAwait(false);
            return result.Item;
        }

        private static async Task RepopulateCacheWhereMissing<T>(string key, Stack<ICache> cacheStack, CacheItem<T> result)
        {
            while (cacheStack != null && cacheStack.Count > 0)
            {
                ICache cacheToPopulate = cacheStack.Pop();
                Debug.WriteLine($"Populating {cacheToPopulate.GetType()} with key={key}.");
                await cacheToPopulate.AddAsync(key, result).ConfigureAwait(false);
            }
        }
    }
}