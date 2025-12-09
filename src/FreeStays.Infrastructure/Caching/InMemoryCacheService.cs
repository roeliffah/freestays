using FreeStays.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FreeStays.Infrastructure.Caching;

/// <summary>
/// In-memory cache service as fallback when Redis is not available
/// </summary>
public class InMemoryCacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public InMemoryCacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _cache.TryGetValue(key, out T? value) ? value : default;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        var options = new MemoryCacheEntryOptions();
        
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);
        }
        
        _cache.Set(key, value, options);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _cache.Remove(key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        return _cache.TryGetValue(key, out _);
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        // In-memory cache doesn't support prefix-based removal easily
        // This is a limitation - for production use Redis
    }
}
