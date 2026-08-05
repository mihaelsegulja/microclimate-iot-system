using MicroclimateIotSystem.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace MicroclimateIotSystem.Infrastructure.Caching;

public class CacheService(IMemoryCache cache) : ICacheService
{
    public bool TryGet<T>(string key, out T value)
    {
        if (cache.TryGetValue(key, out object? cached))
        {
            value = (T)cached!;
            return true;
        }

        value = default!;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan ttl)
        => cache.Set(key, value, ttl);

    public void Remove(string key)
        => cache.Remove(key);
}
