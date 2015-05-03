// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System;
using System.Diagnostics;
using System.Runtime.Caching;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Stebet.LayeredCache.Tests
{
    [TestClass]
    public class LayeredCacheTests
    {
        private Random _random = new Random();

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void MissingCacheThrowsError()
        {
            LayeredCache cache = new LayeredCache();
        }

        [TestMethod]
        public async Task HitsDataStoreWhenEmpty()
        {
            LayeredCache cache = new LayeredCache(new RuntimeCache(Guid.NewGuid().ToString()));
            Assert.IsTrue(Time(async () => await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1))) > 100);
        }

        [TestMethod]
        public async Task ReturnsFromCache()
        {
            LayeredCache cache = new LayeredCache(new RuntimeCache(Guid.NewGuid().ToString()));
            await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1));
            Assert.IsTrue(Time(async () => await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1))) < 10);
        }

        [TestMethod]
        public async Task ShouldHitSecondCacheIfFirstFails()
        {
            RuntimeCache runtimeCache = new RuntimeCache(Guid.NewGuid().ToString());
            LayeredCache cache = new LayeredCache(runtimeCache, new DelayedCache(Guid.NewGuid().ToString()));
            await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1));
            await runtimeCache.RemoveAsync("GetPerson");
            long elapsedTime = Time(async () => await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1)));
            Assert.IsTrue(elapsedTime > 10 && elapsedTime < 60);
        }

        [TestMethod]
        public async Task ShouldRepopulateEmptyCache()
        {
            RuntimeCache runtimeCache = new RuntimeCache(Guid.NewGuid().ToString());
            LayeredCache cache = new LayeredCache(runtimeCache, new DelayedCache(Guid.NewGuid().ToString()));
            await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1));
            await runtimeCache.RemoveAsync("GetPerson");
            await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1));
            long elapsedTime = Time(async () => await cache.GetAsync("GetPerson", () => CreatePersonAsync(), DateTime.UtcNow.AddMinutes(1)));
            Assert.IsTrue(elapsedTime < 10);
        }

        private Task<Person> CreatePersonAsync()
        {
            Thread.Sleep(_random.Next(100, 200));
            return Task.FromResult(new Person { Name = Guid.NewGuid().ToString(), Age = _random.Next(15, 80) });
        }

        private long Time(Func<Task> action)
        {
            Stopwatch timer = Stopwatch.StartNew();
            action().Wait();
            return timer.ElapsedMilliseconds;
        }
    }

    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }

        public override string ToString() => string.Format("{0} ({1})", Name, Age);
    }

    public class DelayedCache : RuntimeCache
    {
        private Random _random = new Random();

        public DelayedCache(string cacheName) : base(cacheName)
        {
        }

        private void Delay() => Thread.Sleep(_random.Next(10, 50));
        
        public override Task AddAsync<T>(string key, CacheItem<T> item)
        {
            Delay();
            return base.AddAsync(key, item);
        }

        public override Task RemoveAsync(string key)
        {
            Delay();
            return base.RemoveAsync(key);
        }

        public override Task ClearAsync()
        {
            Delay();
            return base.ClearAsync();
        }

        public override Task<CacheItem<T>> GetAsync<T>(string key)
        {
            Delay();
            return base.GetAsync<T>(key);
        }
    }
}