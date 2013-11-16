using System;
using System.Collections.Generic;

namespace Stebet.LayeredCache
{
    public class LayeredCache
    {
        private readonly List<ICache> caches = new List<ICache>();

        public LayeredCache(params ICache[] caches)
        {
            if (caches.Length == 0)
            {
                throw new ArgumentException("The LayeredCache needs at least one ICache implementation.");
            }

            this.caches.AddRange(caches);
        }

        public T Get<T>(string key, Func<T> fetchItem, DateTime expiresAt)
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

            // Still haven't found our item, let's get it from our "data store".
            T item = fetchItem();
            result = new CacheItem<T>(item, expiresAt);

            // Let's populate the missing cache items
            while (cacheStack != null && cacheStack.Count > 0)
            {
                cacheStack.Pop().Add(key, result);
            }

            return result.Item;
        }

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
    }
}