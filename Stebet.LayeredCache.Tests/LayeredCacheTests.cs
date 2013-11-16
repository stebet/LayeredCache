using System;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Stebet.LayeredCache.Tests
{
    [TestClass]
    public class LayeredCacheTests
    {
        private Random random = new Random();

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MissingCacheThrowsError()
        {
            LayeredCache cache = new LayeredCache();
        }

        [TestMethod]
        public void HitsDataStoreWhenEmpty()
        {
            LayeredCache cache = new LayeredCache(new RuntimeCache(Guid.NewGuid().ToString()));
            Assert.IsTrue(Time(() => cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1))) > 100);
        }

        [TestMethod]
        public void ReturnsFromCache()
        {
            LayeredCache cache = new LayeredCache(new RuntimeCache(Guid.NewGuid().ToString()));
            cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1));
            Assert.IsTrue(Time(() => cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1))) < 10);
        }

        [TestMethod]
        public void ShouldHitSecondCacheIfFirstFails()
        {
            RuntimeCache runtimeCache = new RuntimeCache(Guid.NewGuid().ToString());
            LayeredCache cache = new LayeredCache(runtimeCache, new DelayedCache());
            cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1));
            runtimeCache.Remove("GetPerson");
            long elapsedTime = Time(() => cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1)));
            Assert.IsTrue(elapsedTime > 10 && elapsedTime < 60);
        }

        [TestMethod]
        public void ShouldRepopulateEmptyCache()
        {
            RuntimeCache runtimeCache = new RuntimeCache(Guid.NewGuid().ToString());
            LayeredCache cache = new LayeredCache(runtimeCache, new DelayedCache());
            cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1));
            runtimeCache.Remove("GetPerson");
            cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1));
            long elapsedTime = Time(() => cache.Get("GetPerson", () => CreatePerson(), DateTime.UtcNow.AddMinutes(1)));
            Assert.IsTrue(elapsedTime < 10);
        }

        private Person CreatePerson()
        {
            Thread.Sleep(random.Next(100, 200));
            return new Person { Name = Guid.NewGuid().ToString(), Age = random.Next(15, 80) };
        }

        private long Time(Action action)
        {
            Stopwatch timer = Stopwatch.StartNew();
            action();
            return timer.ElapsedMilliseconds;
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override string ToString()
        {
            return string.Format("{0} ({1})", this.Name, this.Age);
        }
    }

    public class DelayedCache : ICache
    {
        private Random random = new Random();
        private MemoryCache cache = MemoryCache.Default;

        public void Add<T>(string key, CacheItem<T> item)
        {
            Thread.Sleep(random.Next(10, 50));
            cache.Add(key, item, item.ExpiresAt);
        }

        public void Remove(string key)
        {
            Thread.Sleep(random.Next(10, 50));
            cache.Remove(key);
        }

        public void Clear()
        {
            Thread.Sleep(random.Next(10, 50));
            cache.Trim(100);
        }

        public CacheItem<T> Get<T>(string key)
        {
            Thread.Sleep(random.Next(10, 50));
            return cache.Get(key) as CacheItem<T>;
        }
    }

}
