using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TaskLauncher.WebApp.Server.Services;

public class Cache<T> where T : class
{
    private static readonly object cacheLock = new();
    private readonly int cacheExpirationInDays;
    private readonly IDistributedCache cache;

    public Cache(IDistributedCache cache, int cacheExpirationInDays = 1)
    {
        this.cache = cache;
        this.cacheExpirationInDays = cacheExpirationInDays;
    }

    public void Add(string key, T obj)
    {
        var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromDays(cacheExpirationInDays));

        lock (cacheLock)
        {
            cache.SetString(key, JsonSerializer.Serialize(obj), options);
        }
    }

    public T? Get(string key)
    {
        var item = cache.GetString(key);
        if (item != null)
        {
            return JsonSerializer.Deserialize<T>(item);
        }
        return null;
    }
}
