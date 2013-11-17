using System;
using System.Collections.Generic;
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
        private readonly List<ICache> caches = new List<ICache>();

        /// <summary>
        /// Initializes a new instance of the LayeredCache class.
        /// </summary>
        /// <param name="caches">The ICache implementations to use for the cache.</param>
        public LayeredCache(params ICache[] caches)
        {
            if (caches.Length == 0)
            {
                throw new ArgumentException("The LayeredCache needs at least one ICache implementation.");
            }

            this.caches.AddRange(caches);
        }

        /// <summary>
        /// Checks the caches for a value of type T and populates them if they don't contain the value.
        /// </summary>
        /// <typeparam name="T">The type of the item to fetch from the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetchItem">A method that fetches the value if it is not found in the cache.</param>
        /// <param name="expiresAt">The expiry date of the item.</param>
        /// <returns>A value of type T.</returns>
        public T Get<T>(string key, Func<T> fetchItem, DateTime expiresAt)
        {
            return this.Get(key, fetchItem, x => expiresAt);
        }

        /// <summary>
        /// Checks the caches for a value of type T and populates them if they don't contain the value.
        /// </summary>
        /// <typeparam name="T">The type of the item to fetch from the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetchItem">A method that fetches the value if it is not found in the cache.</param>
        /// <param name="expiryEvaluator">A method that extracts the expiry date from the result.</param>
        /// <returns>A value of type T.</returns>
        public T Get<T>(string key, Func<T> fetchItem, Func<T, DateTime> expiryEvaluator)
        {
            Stack<ICache> cacheStack = null;
            CacheItem<T> result;

            foreach (ICache cache in this.caches)
            {
                result = cache.Get<T>(key);

                if (result != null)
                {
                    // If our item has expired, let's null it out.
                    if (result.ExpiresAt < DateTime.UtcNow)
                    {
                        result = null;
                    }
                }

                if (result != null)
                {
                    // Let's populate the missing cache items
                    while (cacheStack != null && cacheStack.Count > 0)
                    {
                        cacheStack.Pop().Add(key, result);
                    }

                    return result.Item;
                }

                if (cacheStack == null)
                {
                    cacheStack = new Stack<ICache>(this.caches.Count);
                }

                // Expired or item not found, let's mark our cache for population and try the next cache (if available).
                cacheStack.Push(cache);
            }

            // Still haven't found our item, let's get it from our "data store" and extract the expiryDate.
            T item = fetchItem();
            DateTime expiresAt = expiryEvaluator(item);
            result = new CacheItem<T>(item, expiresAt);

            // Let's populate the missing cache items
            while (cacheStack != null && cacheStack.Count > 0)
            {
                cacheStack.Pop().Add(key, result);
            }

            return result.Item;
        }

        /// <summary>
        /// Checks the caches for a value of type T and asynchronously populates them if they don't contain the value.
        /// </summary>
        /// <typeparam name="T">The type of the item to fetch from the cache.</typeparam>
        /// <param name="key">The cache key.</param>
        /// <param name="fetchItemAsync">A method that asynchronously fetches the value if it is not found in the cache.</param>
        /// <param name="expiresAt">The expiry date of the item.</param>
        /// <returns>A Task containing the value of type T.</returns>
        public Task<T> GetAsync<T>(string key, Func<Task<T>> fetchItemAsync, DateTime expiresAt)
        {
            return this.GetAsync(key, fetchItemAsync, x => expiresAt);
        }

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

            foreach (ICache cache in this.caches)
            {
                result = cache.Get<T>(key);

                if (result != null)
                {
                    // If our item has expired, let's null it out.
                    if (result.ExpiresAt < DateTime.UtcNow)
                    {
                        result = null;
                    }
                }

                if (result != null)
                {
                    // Let's populate the missing cache items
                    while (cacheStack != null && cacheStack.Count > 0)
                    {
                        cacheStack.Pop().Add(key, result);
                    }

                    return result.Item;
                }

                if (cacheStack == null)
                {
                    cacheStack = new Stack<ICache>(this.caches.Count);
                }

                // Expired or item not found, let's mark our cache for population and try the next cache (if available).
                cacheStack.Push(cache);
            }

            // Still haven't found our item, let's get it from our "data store" and extract the expiryDate.
            T item = await fetchItemAsync().ConfigureAwait(false);
            DateTime expiresAt = expiryEvaluator(item);
            result = new CacheItem<T>(item, expiresAt);

            // Let's populate the missing cache items
            while (cacheStack != null && cacheStack.Count > 0)
            {
                cacheStack.Pop().Add(key, result);
            }

            return result.Item;
        }
    }
}