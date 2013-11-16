namespace Stebet.LayeredCache
{
    public interface ICache
    {
        void Add<T>(string key, CacheItem<T> item);
        void Remove(string key);
        void Clear();
        CacheItem<T> Get<T>(string key);
    }
}
