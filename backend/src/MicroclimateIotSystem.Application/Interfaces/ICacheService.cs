namespace MicroclimateIotSystem.Application.Interfaces;

public interface ICacheService
{
    bool TryGet<T>(string key, out T value);
    void Set<T>(string key, T value, TimeSpan ttl);
    void Remove(string key);
}
