using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace TaskLauncher.Common.Services;

public class CacheConfiguration<T>
{
    public TimeSpan AbsoluteExpiration { get; set; }
}

public class Cache<T> where T : class
{
    private static readonly object cacheLock = new();
    private readonly IDistributedCache cache;
    private readonly CacheConfiguration<T> config;
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public Cache(IDistributedCache cache, CacheConfiguration<T> config)
    {
        this.cache = cache;
        this.config = config;
    }

    public void Add(string key, T obj)
    {
        var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(config.AbsoluteExpiration);

        lock (cacheLock)
        {
            cache.SetString(key, JsonSerializer.Serialize(obj), options);
        }
    }

    public async Task AddAsync(string key, T obj)
    {
        var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(config.AbsoluteExpiration);

        await semaphore.WaitAsync();
        await cache.SetStringAsync(key, JsonSerializer.Serialize(obj), options);
        semaphore.Release();
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

    public async Task<T?> GetAsync(string key)
    {
        var item = await cache.GetStringAsync(key);
        if (item != null)
        {
            return JsonSerializer.Deserialize<T>(item);
        }
        return null;
    }

    public void Update(string key, T value)
    {
        cache.Remove(key);
        Add(key, value);
    }
}